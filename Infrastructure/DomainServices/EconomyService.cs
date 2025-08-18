using SkyHorizont.Domain.Economy;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Simple per-planet credit ledger (separate from resources).
    /// </summary>
    public class EconomyService : IEconomyService
    {
        private readonly IPlanetEconomyRepository _eco;
        private readonly IPlanetRepository _planets;
        private readonly IFleetRepository _fleets;
        private readonly ICharacterRepository _characters;
        private readonly IFactionFundsRepository _factionFunds;
        private readonly IFactionService _factionInfo;
        private readonly IFactionTaxService _factionTaxService;
        private readonly IGameClockService _clock;

        private readonly EconomyTuning _cfg;

        // ---- Tuning knobs that remain local (value model & smuggling) ----
        private const int TradeBaseUnitValue       = 5;    // value per route capacity unit
        private const double SmugglingCutToPirates = 0.65; // pirates receive 65% of smuggled value
        private const double SmugglingLossAtSource = 0.15; // source planet loses 15% to leakage/corruption
        private const int MinimumTariffPercent     = 0;    // sanity guards
        private const int MaximumTariffPercent     = 75;

        private static readonly Dictionary<Rank, int> SalaryByRank = new()
        {
            { Rank.Civilian, 0 },{ Rank.Courtesan, 10 }, { Rank.Lieutenant, 20 }, { Rank.Captain, 40 },
            { Rank.Major, 60 }, { Rank.Colonel, 90 }, { Rank.General, 140 }, { Rank.Leader, 250 }
        };

        public EconomyService(
            IPlanetEconomyRepository eco,
            IPlanetRepository planets,
            IFleetRepository fleets,
            ICharacterRepository characters,
            IFactionFundsRepository factionFunds,
            IFactionService factionInfo,
            IFactionTaxService factionTaxService,
            IGameClockService clock,
            EconomyTuning? cfg = null) // NEW: optional tuning injected
        {
            _eco = eco;
            _planets = planets;
            _fleets = fleets;
            _characters = characters;
            _factionFunds = factionFunds;
            _factionInfo = factionInfo;
            _factionTaxService = factionTaxService;
            _clock = clock;
            _cfg = cfg ?? new EconomyTuning(); // default matches old behavior
        }

        // -------------------- Public API --------------------

        public void EndOfTurnUpkeep()
        {
            ProcessTaxes();

            // Upkeep
            DoFleetUpkeep();
            DoPlanetUpkeep();
            DoCharacterSalaries();

            // Trade & Tariffs (and Smuggling)
            ProcessTradeRoutes();

            // Loans
            ProcessLoans();
        }

        public void SetTariff(Guid factionId, int percent)
        {
            percent = Math.Clamp(percent, MinimumTariffPercent, MaximumTariffPercent);
            _eco.SetTariffPolicy(factionId, new TariffPolicy(factionId, percent));
        }

        public int GetTariff(Guid factionId)
            => _eco.GetTariffPolicy(factionId)?.Percent ?? 0;

        public TariffPolicy? GetTariffPolicy(Guid factionId)
            => _eco.GetTariffPolicy(factionId);

        public Guid CreateTradeRoute(Guid fromPlanetId, Guid toPlanetId, int capacity, bool smuggling = false)
        {
            if (capacity <= 0) capacity = 1;
            var route = new TradeRoute(Guid.NewGuid(), fromPlanetId, toPlanetId, capacity, smuggling);
            _eco.SetTradeRoutes(route);
            _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                "TradeRouteCreated", null, 0, $"{fromPlanetId}→{toPlanetId}, cap={capacity}, smuggling={smuggling}"));
            return route.Id;
        }

        public void RemoveTradeRoute(Guid routeId)
        {
            _eco.RemoveTradeRoute(routeId);
            _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                "TradeRouteRemoved", null, 0, routeId.ToString()));
        }

        public void RecordBlackMarketTrade(Guid planetId, int credits, Guid counterpartyFactionId, string note = "Black market trade")
        {
            var planet = _planets.GetById(planetId);
            if (planet is null || credits <= 0) return;

            // Source planet loses some value to corruption
            int leakage = (int)Math.Round(credits * SmugglingLossAtSource);
            int piratesTake = (int)Math.Round((credits - leakage) * SmugglingCutToPirates);

            // Debit planet (if possible)
            _eco.TryDebitBudget(planet.Id, leakage + piratesTake);
            _planets.Save(planet);

            // Credit pirates (faction funds)
            _factionFunds.AddBalance(counterpartyFactionId, piratesTake);

            _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                "BlackMarket", planetId, -(leakage + piratesTake), $"{note}: leakage={leakage}, pirates={piratesTake}"));
        }

        public Guid CreateLoan(LoanAccountType type, Guid ownerId, int principal, double monthlyInterestRate, int termMonths)
        {
            if (principal <= 0 || monthlyInterestRate < 0 || termMonths <= 0)
                throw new ArgumentException("Invalid loan parameters.");

            var loan = new Loan(Guid.NewGuid(), type, ownerId, principal, monthlyInterestRate, termMonths, _clock.CurrentYear, _clock.CurrentMonth);
            _eco.SetLoan(loan);

            // Disburse principal
            CreditOwner(loan.AccountType, loan.OwnerId, principal);
            _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                "LoanDisbursed", ownerId, principal, $"{type} principal {principal}"));

            return loan.Id;
        }

        public void MakeLoanPayment(Guid loanId, int amount)
        {
            var loan = _eco.GetLoan(loanId);
            if (loan == null || amount <= 0) return;

            if (!DebitOwner(loan.AccountType, loan.OwnerId, amount)) return; // insufficient funds
            int paid = loan.MakePayment(amount);

            _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                "LoanPayment", loan.OwnerId, -paid, $"{loan.AccountType} paid {paid}"));
        }

        public void CreditPlanetBudget(Guid planetId, int credits)
        {
            if (credits <= 0) return;
            _eco.AddBudget(planetId, credits);
        }

        public int GetPlanetBudget(Guid planetId)
        {
            return _eco.GetPlanetBudget(planetId);
        }

        // -------------------- Internals --------------------

        private void ProcessTaxes()
        {
            foreach (var planet in _planets.GetAll())
            {
                var baseTaxRate = planet.BaseTaxRate;
                _factionTaxService.TaxPlanet(planet.Id, baseTaxRate);
            }
        }

        private void DoFleetUpkeep()
        {
            foreach (var fleet in _fleets.GetAll())
            {
                Character? commander = null;
                if (fleet.AssignedCharacterId.HasValue && fleet.AssignedCharacterId.Value != Guid.Empty)
                    commander = _characters.GetById(fleet.AssignedCharacterId.Value);

                var maintPct = FleetMaintenancePctFor(fleet, commander); // NEW
                int upkeep = (int)Math.Ceiling(fleet.Ships.Sum(s => s.Cost * maintPct));
                if (upkeep <= 0) continue;

                var factionId = fleet.FactionId;
                if (_factionFunds.GetBalance(factionId) >= upkeep)
                {
                    _factionFunds.DeductBalance(factionId, upkeep);
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "UpkeepFleet", factionId, -upkeep, $"Fleet {fleet.Id} upkeep from faction (pct {maintPct:P1})"));
                }
                else
                {
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "UpkeepFleetUnpaid", factionId, 0, $"Fleet {fleet.Id} unpaid upkeep {upkeep} (pct {maintPct:P1})"));
                }
            }
        }

        private void DoPlanetUpkeep()
        {
            foreach (var planet in _planets.GetAll())
            {
                Character? governor = null;
                if (planet.GovernorId.HasValue && planet.GovernorId.Value != Guid.Empty)
                    governor = _characters.GetById(planet.GovernorId.Value);

                int infraUpkeep = InfraUpkeepFor(planet, governor); // NEW
                if (infraUpkeep <= 0) continue;

                if (!_eco.TryDebitBudget(planet.Id, infraUpkeep))
                {
                    var factionId = planet.FactionId;
                    if (_factionFunds.GetBalance(factionId) >= infraUpkeep)
                    {
                        _factionFunds.DeductBalance(factionId, infraUpkeep);
                        _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                            "UpkeepPlanetFactionCovered", factionId, -infraUpkeep, $"Covered infra upkeep for {planet.Id} (adj)"));
                    }
                    else
                    {
                        _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                            "UpkeepPlanetUnpaid", planet.Id, 0, $"Unpaid infra upkeep {infraUpkeep} (adj)"));
                    }
                }
                else
                {
                    _planets.Save(planet);
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "UpkeepPlanet", planet.Id, -infraUpkeep, $"Infra upkeep {infraUpkeep} (adj)"));
                }
            }
        }

        private void DoCharacterSalaries()
        {
            foreach (var c in _characters.GetAll())
            {
                if (!c.IsAlive) continue;
                if (!SalaryByRank.TryGetValue(c.Rank, out var salary) || salary <= 0) continue;

                var factionId = _factionInfo.GetFactionIdForCharacter(c.Id);
                if (_factionFunds.GetBalance(factionId) >= salary)
                {
                    _factionFunds.DeductBalance(factionId, salary);
                    c.Credit(salary);
                    _characters.Save(c);

                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "Salary", c.Id, salary, $"Paid salary {salary}, faction {factionId} debited"));
                }
                else
                {
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "SalaryUnpaid", c.Id, 0, $"Unpaid salary {salary} from faction {factionId}"));
                }
            }
        }

        private void ProcessTradeRoutes()
        {
            foreach (var route in _eco.GetTradeRoutes())
            {
                var from = _planets.GetById(route.FromPlanetId);
                var to   = _planets.GetById(route.ToPlanetId);
                if (from is null || to is null) continue;

                double infraFactor = Math.Max(1.0, (from.InfrastructureLevel + to.InfrastructureLevel) / 100.0 * 2.0);
                double distanceFactor = DistanceValueFactor(from.Id, to.Id);
                int grossValue = (int)Math.Round(route.Capacity * TradeBaseUnitValue * infraFactor * distanceFactor);

                if (route.IsSmuggling)
                {
                    var pirateFaction = FindPirateFactionNear(from.FactionId) ?? from.FactionId;
                    int leakage = (int)Math.Round(grossValue * SmugglingLossAtSource);
                    int piratesTake = (int)Math.Round((grossValue - leakage) * SmugglingCutToPirates);

                    _eco.TryDebitBudget(from.Id, leakage + piratesTake);
                    _factionFunds.AddBalance(pirateFaction, piratesTake);

                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "Smuggling", from.Id, -(leakage + piratesTake), $"Route {route.Id} → pirates {piratesTake}, leakage {leakage}"));
                }
                else
                {
                    int half = grossValue / 2;
                    _eco.AddBudget(from.Id, half);
                    _eco.AddBudget(to.Id, grossValue - half);

                    ApplyTariffOnPlanet(from, half, route.Id);
                    ApplyTariffOnPlanet(to, grossValue - half, route.Id);

                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "Trade", null, grossValue, $"Route {route.Id} value {grossValue} (dist {distanceFactor:F2})"));
                }
            }
        }

        private void ApplyTariffOnPlanet(Planet planet, int credited, Guid routeId)
        {
            var policy = _eco.GetTariffPolicy(planet.FactionId);
            if (policy is null || policy.Percent <= 0) return;

            int tax = (int)Math.Floor(credited * (policy.Percent / 100.0));
            if (tax <= 0) return;

            if (_eco.TryDebitBudget(planet.Id, tax))
            {
                _planets.Save(planet);
                _factionFunds.AddBalance(planet.FactionId, tax);

                _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                    "Tariff", planet.FactionId, tax, $"Route {routeId} tariff {policy.Percent}% from {planet.Id} = {tax}"));
            }
        }

        private void ProcessLoans()
        {
            foreach (var loan in _eco.GetLoans())
            {
                if (loan.IsDefaulted || loan.IsFullyRepaid) continue;

                int interest = loan.AccrueInterest();
                _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                    "LoanInterest", loan.OwnerId, interest, $"{loan.AccountType} interest accrued"));

                int due = Math.Max(1, (int)Math.Ceiling(loan.RemainingPrincipal * 0.10));
                if (DebitOwner(loan.AccountType, loan.OwnerId, due))
                {
                    int paid = loan.MakePayment(due);
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "LoanAutoPayment", loan.OwnerId, -paid, $"{loan.AccountType} auto‑pay {paid}"));
                }
                else
                {
                    loan.MarkDefault();
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "LoanDefault", loan.OwnerId, 0, $"{loan.AccountType} defaulted with {loan.RemainingPrincipal} remaining"));
                }
            }
        }

        // --------- helpers for credit/debit across account types ----------

        private void CreditOwner(LoanAccountType type, Guid ownerId, int amount)
        {
            switch (type)
            {
                case LoanAccountType.Planet:
                    var p = _planets.GetById(ownerId);
                    if (p is null) return;
                    _eco.AddBudget(p.Id, amount);
                    break;
                case LoanAccountType.Faction:
                    _factionFunds.AddBalance(ownerId, amount);
                    break;
                case LoanAccountType.Character:
                    var c = _characters.GetById(ownerId);
                    if (c is null) return;
                    c.Credit(amount);
                    _characters.Save(c);
                    break;
            }
        }

        private bool DebitOwner(LoanAccountType type, Guid ownerId, int amount)
        {
            switch (type)
            {
                case LoanAccountType.Planet:
                    var p = _planets.GetById(ownerId);
                    if (p is null) return false;
                    var ok = _eco.TryDebitBudget(p.Id, amount);
                    return ok;

                case LoanAccountType.Faction:
                    if (_factionFunds.GetBalance(ownerId) < amount) return false;
                    _factionFunds.DeductBalance(ownerId, amount);
                    return true;

                case LoanAccountType.Character:
                    var c = _characters.GetById(ownerId);
                    if (c is null) return false;
                    var paid = c.Deduct(amount);
                    if (paid) _characters.Save(c);
                    return paid;

                default:
                    return false;
            }
        }

        private Guid? FindPirateFactionNear(Guid fallbackFaction)
        {
            // TODO: integrate a pirate directory or starmap proximity.
            // For now, return null to fall back to controlling faction when needed.
            return null;
        }

        // -------------------- NEW helpers (rates & distance) --------------------

        private double FleetMaintenancePctFor(Fleet fleet, Character? commander)
        {
            // Start from configured base
            double pct = _cfg.BaseShipMaintPct;
            if (commander is null) return pct;

            // Military skill reduces maintenance up to configured cap
            double skillFactor = 1.0 - (commander.Skills.Military / 100.0) * _cfg.FleetMaintSkillReductionPer100;
            // Conscientiousness reduces waste/leakage a bit
            double conscFactor = 1.0 - (commander.Personality.Conscientiousness / 100.0) * _cfg.FleetMaintConscReductionPer100;

            var result = pct * skillFactor * conscFactor;
            return Math.Clamp(result, 0.005, 0.10);
        }

        private int InfraUpkeepFor(Planet planet, Character? governor)
        {
            var baseUpkeep = planet.InfrastructureLevel * _cfg.BaseInfraUpkeepPerLvl;
            if (baseUpkeep <= 0) return 0;
            if (governor is null) return baseUpkeep;

            double skillFactor = 1.0 - (governor.Skills.Economy / 100.0) * _cfg.GovInfraSkillReductionPer100;
            double conscFactor = 1.0 - (governor.Personality.Conscientiousness / 100.0) * _cfg.GovConscReductionPer100;

            var adjusted = baseUpkeep * Math.Max(0.5, skillFactor * conscFactor);
            return (int)Math.Ceiling(adjusted);
        }

        private double DistanceValueFactor(Guid fromPlanetId, Guid toPlanetId)
        {
            // Stub for now (no extra dependency). 
            // ToDo: Integrate starmap later.
            // Return 1.0 → your current behavior.
            return 1.0;
        }
    }
}

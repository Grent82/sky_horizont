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

        
        // ---- Tuning knobs (easy to externalize later) ----
        private const double ShipMonthlyMaintenancePct = 0.02;   // ToDo: make configurable (based on character skills and personality): 2% of ship Cost per month
        private const int PlanetInfraUpkeepPerLevel    = 3;      // ToDo: make configurable (based on character skills and personality): credits per infra‑level
        private static readonly Dictionary<Rank, int> SalaryByRank = new()
        {
            { Rank.Civilian, 0 }, { Rank.Lieutenant, 20 }, { Rank.Captain, 40 },
            { Rank.Major, 60 }, { Rank.Colonel, 90 }, { Rank.General, 140 }, { Rank.Leader, 0 }
        };
        private const int TradeBaseUnitValue          = 5;       // value per route capacity unit
        private const double SmugglingCutToPirates    = 0.65;    // pirates receive 65% of smuggled value
        private const double SmugglingLossAtSource    = 0.15;    // source planet loses 15% to leakage/corruption
        private const int MinimumTariffPercent        = 0;       // sanity guards
        private const int MaximumTariffPercent        = 75;

        // ToDo: Planet and faction budged
        public EconomyService(
            IPlanetEconomyRepository eco,
            IPlanetRepository planets,
            IFleetRepository fleets,
            ICharacterRepository characters,
            IFactionFundsRepository factionFunds,
            IFactionService factionInfo,
            IFactionTaxService factionTaxService,
            IGameClockService clock)
        {
            _eco = eco;
            _planets = planets;
            _fleets = fleets;
            _characters = characters;
            _factionFunds = factionFunds;
            _factionInfo = factionInfo;
            _factionTaxService = factionTaxService;
            _clock = clock;
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
            if ( loan == null || amount <= 0) return;

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
                // ToDo: The % could be stored per planet or per faction policy
                var baseTaxRate = planet.BaseTaxRate;
                _factionTaxService.TaxPlanet(planet.Id, baseTaxRate);
            }
        }

        private void DoFleetUpkeep()
        {
            foreach (var fleet in _fleets.GetAll())
            {
                // maintenance equals sum(ship.Cost * pct)
                // ToDo: maintance, maybe half of the cost or dependend on commanders or leader
                int upkeep = (int)Math.Ceiling(fleet.Ships.Sum(s => s.Cost * ShipMonthlyMaintenancePct));
                if (upkeep <= 0) continue;

                // Try charge faction treasury first; if none, try nearest owned planet?
                var factionId = fleet.FactionId;
                if (_factionFunds.GetBalance(factionId) >= upkeep)
                {
                    _factionFunds.DeductBalance(factionId, upkeep);
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "UpkeepFleet", factionId, -upkeep, $"Fleet {fleet.Id} upkeep from faction"));
                }
                else
                {
                    // fallback: no money → technical debt (you could mark morale/supply penalties elsewhere)
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "UpkeepFleetUnpaid", factionId, 0, $"Fleet {fleet.Id} unpaid upkeep {upkeep}"));
                }
            }
        }

        private void DoPlanetUpkeep()
        {
            foreach (var planet in _planets.GetAll())
            {
                int infraUpkeep = planet.InfrastructureLevel * PlanetInfraUpkeepPerLevel;
                if (infraUpkeep <= 0) continue;

                // Pay from planet budget; if not enough, attempt faction cover
                if (!_eco.TryDebitBudget(planet.Id, infraUpkeep))
                {
                    var factionId = planet.ControllingFactionId;
                    if (_factionFunds.GetBalance(factionId) >= infraUpkeep)
                    {
                        _factionFunds.DeductBalance(factionId, infraUpkeep);
                        _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                            "UpkeepPlanetFactionCovered", factionId, -infraUpkeep, $"Covered infra upkeep for {planet.Id}"));
                    }
                    else
                    {
                        _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                            "UpkeepPlanetUnpaid", planet.Id, 0, $"Unpaid infra upkeep {infraUpkeep}"));
                    }
                }
                else
                {
                    _planets.Save(planet);
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "UpkeepPlanet", planet.Id, -infraUpkeep, $"Infra upkeep {infraUpkeep}"));
                }
            }
        }

        private void DoCharacterSalaries()
        {
            foreach (var c in _characters.GetAll())
            {
                if (!c.IsAlive) continue;
                if (!SalaryByRank.TryGetValue(c.Rank, out var salary) || salary <= 0) continue;

                // Pay from faction treasury; if not enough, skip
                var factionId = _factionInfo.GetFactionIdForCharacter(c.Id);
                if (_factionFunds.GetBalance(factionId) >= salary)
                {
                    _factionFunds.DeductBalance(factionId, salary);
                    c.Credit(salary); // character personal funds
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

                // Very simple value model: capacity * (avg infra / 50) * base unit
                double infraFactor = Math.Max(1.0, (from.InfrastructureLevel + to.InfrastructureLevel) / 100.0 * 2.0);
                int grossValue = (int)Math.Round(route.Capacity * TradeBaseUnitValue * infraFactor);

                if (route.IsSmuggling)
                {
                    // Smuggling: bleed the source planet; pirates get most of it
                    var pirateFaction = FindPirateFactionNear(from.ControllingFactionId) ?? from.ControllingFactionId;
                    int leakage = (int)Math.Round(grossValue * SmugglingLossAtSource);
                    int piratesTake = (int)Math.Round((grossValue - leakage) * SmugglingCutToPirates);

                    _eco.TryDebitBudget(from.Id, leakage + piratesTake);
                    _factionFunds.AddBalance(pirateFaction, piratesTake);

                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "Smuggling", from.Id, -(leakage + piratesTake), $"Route {route.Id} → pirates {piratesTake}, leakage {leakage}"));
                }
                else
                {
                    // Legal trade: split gross to planets, then apply tariff to controlling factions
                    int half = grossValue / 2;
                    _eco.AddBudget(from.Id, half);
                    _eco.AddBudget(to.Id, grossValue - half);

                    // Apply tariffs by each controlling faction, if any
                    ApplyTariffOnPlanet(from, half, route.Id);
                    ApplyTariffOnPlanet(to, grossValue - half, route.Id);

                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "Trade", null, grossValue, $"Route {route.Id} value {grossValue}"));
                }
            }
        }

        private void ApplyTariffOnPlanet(Planet planet, int credited, Guid routeId)
        {
            var policy = _eco.GetTariffPolicy(planet.ControllingFactionId);
            if (policy is null || policy.Percent <= 0) return;

            int tax = (int)Math.Floor(credited * (policy.Percent / 100.0));
            if (tax <= 0) return;

            if (_eco.TryDebitBudget(planet.Id, tax))
            {
                _planets.Save(planet);
                _factionFunds.AddBalance(planet.ControllingFactionId, tax);

                _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                    "Tariff", planet.ControllingFactionId, tax, $"Route {routeId} tariff {policy.Percent}% from {planet.Id} = {tax}"));
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

                // Auto‑payment attempt: 10% of remaining (rounded up), min 1
                int due = Math.Max(1, (int)Math.Ceiling(loan.RemainingPrincipal * 0.10));
                if (DebitOwner(loan.AccountType, loan.OwnerId, due))
                {
                    int paid = loan.MakePayment(due);
                    _eco.AddEventLog(new EconomyEvent(_clock.CurrentYear, _clock.CurrentMonth,
                        "LoanAutoPayment", loan.OwnerId, -paid, $"{loan.AccountType} auto‑pay {paid}"));
                }
                else
                {
                    // No funds: mark default (you could add penalties elsewhere)
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
            // ToDo: pick pirate factions tracked, or now, return null to credit fallbackFaction if needed.
            return null;
        }
    }
}


# Example Usages

## Lineage

```csharp
// Application layer code, after loading needed repositories via DI

var tree = await _lineageRepo.FindByChildId(playerCommanderId);
if (tree == null)
    throw new NotFoundException("Lineage missing");

var siblings = _familyTree.GetSiblings(playerCommanderId);
var grandparents = _familyTree.GetGrandparents(playerCommanderId);

// Pass those GUIDs to your UI layer, which can fetch Repository details to obtain names, traits, etc.
```

## Pirate Raid Contract

```csharp
// Application layer (e.g. UseCase/CommandHandler)
var faction = await _factionRepo.Find(factionId);
var fleet = await _fleetRepo.Find(fleetId);

fleet.HirePrivateers(
    pirateFactionId: pirates.Id,
    creditCost: pirateOffer.Cost,
    shipCount: pirateOffer.ShipCount,
    repo: _pirateContractRepo
);

await _factionRepo.Update(faction);
await _fleetRepo.AddOrUpdate(fleet);
```

## Apply Fleet Research Bonus

```csharp
foreach(var ship in fleet.Ships)
{
    ship.ApplyResearchBonus(researchBonus.AttackPercentBonus, researchBonus.DefensePercentBonus);
}
```

## Battle Integration & Planet Conquest


```csharp
var planet = _planetRepo.Find(id);
var attackingStrength = attackerFleet.CalculateStrength().MilitaryPower + attackerBonus;
var defenseStrength = planet.EffectiveDefense(defBonusPct);

if (attackingStrength > defenseStrength)
{
    planet.ConqueredBy(attackerFleet.FactionId, new BattleResult { WinnerFleet = attackerFleet, LoserFleet = planet.StationedFleet, HoursOfOccupation = 5 });
    // trigger events, update Commander merits, resources captured, etc.
}
else
{
    // defender holds: loser fleet destroyed, attacker damaged
}
```


## Funds implementation in infrastructure (wired via dependency injection)

```
public class FundsService : IFundsService
{
    private readonly IFactionFundsRepository _repo;
    public FundsService(IFactionFundsRepository repo) { ... }

    public bool HasFunds(Guid factionId, int amount)
        => _repo.GetBalance(factionId) >= amount;

    public void Deduct(Guid f, int a)
        => _repo.AddBalance(f, -a);

    public void Credit(Guid f, int a)
        => _repo.AddBalance(f, a);
}
```




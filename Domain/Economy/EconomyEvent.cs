using System;

namespace SkyHorizont.Domain.Economy
{
    public sealed record EconomyEvent(
        int Year, int Month,
        string Kind,           // e.g., "Upkeep", "Tariff", "LoanInterest", "Smuggling"
        Guid? OwnerId,         // planet/faction/character
        int Amount,            // positive credits (credited), negative (debited)
        string Note
    );
}

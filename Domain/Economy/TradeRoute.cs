using System;

namespace SkyHorizont.Domain.Economy
{
    public sealed record TradeRoute(
        Guid Id,
        Guid FromPlanetId,
        Guid ToPlanetId,
        int Capacity,       // abstract “throughput” per turn
        bool IsSmuggling    // black‑market if true
    );
}

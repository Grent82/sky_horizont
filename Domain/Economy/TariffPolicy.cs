using System;

namespace SkyHorizont.Domain.Economy
{
    public sealed record TariffPolicy(Guid FactionId, int Percent); // 0..100
}

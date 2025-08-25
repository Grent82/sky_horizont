using System.Collections.Generic;
using SkyHorizont.Domain.Factions;

namespace SkyHorizont.Domain.Fleets
{
    public static class DoctrineTemplates
    {
        public static IReadOnlyDictionary<ShipClass, double> GetRatios(FactionDoctrine doctrine)
            => doctrine switch
            {
                FactionDoctrine.Carrier => new Dictionary<ShipClass, double>
                {
                    { ShipClass.Carrier, 0.5 },
                    { ShipClass.Frigate, 0.3 },
                    { ShipClass.Scout, 0.2 }
                },
                FactionDoctrine.TradeProtection => new Dictionary<ShipClass, double>
                {
                    { ShipClass.Destroyer, 0.3 },
                    { ShipClass.Freighter, 0.5 },
                    { ShipClass.Scout, 0.2 }
                },
                _ => new Dictionary<ShipClass, double>
                {
                    { ShipClass.Frigate, 0.4 },
                    { ShipClass.Freighter, 0.4 },
                    { ShipClass.Scout, 0.2 }
                }
            };
    }
}

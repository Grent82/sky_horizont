namespace SkyHorizont.Domain.Fleets
{
    public interface IFleetService
    {
        FleetStrength GetStrength(Guid fleetId);
        IReadOnlyList<Guid> GetFleetShips(Guid fleetId);
        bool HasActiveOrders(Guid fleetId);
        // Other privileged queries or operations across aggregates
    }
}
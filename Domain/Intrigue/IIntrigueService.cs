namespace SkyHorizont.Domain.Intrigue
{
    public interface IIntrigueService
    {
        void TickPlots(); // progress, recruit, expose
        void AttemptDefection(Guid actorId, Guid newFactionId);
        void AttemptBlackmail(Guid holderId, Guid targetId, Secret secret);
    }
}
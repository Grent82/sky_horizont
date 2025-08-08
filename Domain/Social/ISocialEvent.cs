namespace SkyHorizont.Domain.Social
{
    public interface ISocialEvent
    {
        Guid ActorId { get; }
        Guid? TargetId { get; }
        string Type { get; }
        int Impact { get; } // magnitude, e.g., opinion delta or scandal score
    }
}
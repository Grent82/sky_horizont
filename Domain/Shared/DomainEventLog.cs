namespace SkyHorizont.Domain.Shared
{
    public sealed record DomainEventLog(string Kind, Guid SubjectId, string? Note);
}
namespace SkyHorizont.Domain.Intrigue
{
    public record Secret(Guid SubjectId, string Category, int Severity, Guid? HolderId = null);
}
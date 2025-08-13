namespace SkyHorizont.Domain.Services
{
    public sealed record IntelReport(
        Guid Id,
        Guid SourceCharacterId,
        Guid TargetFactionId,
        string Summary,
        int IntelValue,
        int Year,
        int Month
    );
}

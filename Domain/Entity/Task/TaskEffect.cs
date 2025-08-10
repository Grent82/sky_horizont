namespace SkyHorizont.Domain.Entity.Task
{
    /// <summary>Marker + base for all task results.</summary>
    public abstract record TaskEffect(Guid TaskId, Guid CharacterId, int CompletedYear, int CompletedMonth);

    // Research outcome: unlock/points/“tech progress” payload
    public sealed record ResearchUnlockEffect(
        Guid TaskId,
        Guid CharacterId,
        int CompletedYear,
        int CompletedMonth,
        string TechnologyName,
        int ResearchPoints
    ) : TaskEffect(TaskId, CharacterId, CompletedYear, CompletedMonth);

    // Espionage outcome: intel report payload
    public sealed record IntelReportEffect(
        Guid TaskId,
        Guid CharacterId,
        int CompletedYear,
        int CompletedMonth,
        Guid TargetFactionId,
        string Summary,
        int IntelValue
    ) : TaskEffect(TaskId, CharacterId, CompletedYear, CompletedMonth);

    // Governance outcome: economics & stability payload
    public sealed record GovernanceChangeEffect(
        Guid TaskId,
        Guid CharacterId,
        int CompletedYear,
        int CompletedMonth,
        Guid PlanetId,
        double StabilityDelta,
        int CreditsGenerated
    ) : TaskEffect(TaskId, CharacterId, CompletedYear, CompletedMonth);
}

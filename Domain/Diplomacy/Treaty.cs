namespace SkyHorizont.Domain.Diplomacy
{
    public record Treaty(Guid Id, Guid FactionA, Guid FactionB, TreatyType Type, int StartTurn, int? EndTurn);
}
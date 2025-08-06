namespace SkyHorizont.Domain.Factions;

public record DiplomaticStanding
{
    private readonly int _value;
    public DiplomaticStanding(int value)
    {
        _value = Math.Clamp(value, -100, +100);
    }

    public DiplomaticStanding Adjust(int delta) =>
        new DiplomaticStanding(_value + delta);

    public int Value => _value;
}
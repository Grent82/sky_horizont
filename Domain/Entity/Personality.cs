namespace SkyHorizont.Domain.Entity;

public record Personality(PersonalityType Type, int Loyalty, int Boldness, int Diplomacy)
{
    public int CheckCompatibility(Personality other) =>
        100 - Math.Abs((int)Type - (int)other.Type) * 20
        + (Loyalty + Boldness + Diplomacy) / 3;
}
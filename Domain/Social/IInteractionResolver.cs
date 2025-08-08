namespace SkyHorizont.Domain.Social
{
    public interface IInteractionResolver
    {
        IEnumerable<ISocialEvent> Resolve(IEnumerable<CharacterIntent> intents);
    }
}
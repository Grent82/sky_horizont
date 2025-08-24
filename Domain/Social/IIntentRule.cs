namespace SkyHorizont.Domain.Social
{
    /// <summary>
    /// Generates scored intents for a single intent type.
    /// </summary>
    public interface IIntentRule
    {
        IEnumerable<ScoredIntent> Generate(IntentContext context);
    }
}

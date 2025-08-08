namespace SkyHorizont.Domain.Social
{
    public interface IInteractionResolver
    {
        public interface IInteractionResolver
        {
            /// <summary>
            /// Resolves a planned intent into concrete social events,
            /// updates opinions/affection, and persists secrets if any.
            /// </summary>
            IEnumerable<ISocialEvent> Resolve(CharacterIntent intent, int turn);
        }
    }
}
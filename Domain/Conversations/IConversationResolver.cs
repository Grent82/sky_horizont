namespace SkyHorizont.Domain.Conversation
{
    public interface IConversationResolver
    {
        // ToDo: Communitytion implementation
        bool Resolve(ConversationAttempt attempt, out int opinionDelta, out string transcriptKey);
    }
}

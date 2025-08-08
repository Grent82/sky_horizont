namespace SkyHorizont.Domain.Conversation
{
    public interface IConversationResolver
    {
        bool Resolve(ConversationAttempt attempt, out int opinionDelta, out string transcriptKey);
    }
}

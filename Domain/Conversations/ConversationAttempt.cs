namespace SkyHorizont.Domain.Conversation
{
    public record ConversationAttempt(Guid SpeakerId, Guid ListenerId, DialogueAct Act, int Stakes);
}

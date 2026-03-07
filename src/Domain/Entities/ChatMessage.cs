using InstruaMe.Domain.Entities.Base;

namespace InstruaMe.Domain.Entities
{
    public sealed class ChatMessage : EntityBase
    {
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderRole { get; set; }
        public string Content { get; set; }
        public bool Read { get; set; }

        public Conversation Conversation { get; set; }

        public ChatMessage() { }

        public ChatMessage(Guid conversationId, Guid senderId, string senderRole, string content)
        {
            ConversationId = conversationId;
            SenderId = senderId;
            SenderRole = senderRole;
            Content = content;
            Read = false;
            Register();
        }
    }
}

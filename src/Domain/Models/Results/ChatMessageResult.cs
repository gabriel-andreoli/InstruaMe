namespace InstruaMe.Domain.Models.Results
{
    public sealed class ChatMessageResult
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderRole { get; set; }
        public string Content { get; set; }
        public bool Read { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
    }
}

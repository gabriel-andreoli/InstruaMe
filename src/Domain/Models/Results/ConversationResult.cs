namespace InstruaMe.Domain.Models.Results
{
    public sealed class ConversationResult
    {
        public Guid Id { get; set; }
        public Guid InstructorId { get; set; }
        public string InstructorName { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
    }
}

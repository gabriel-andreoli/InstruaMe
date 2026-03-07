namespace InstruaMe.Domain.Models.Results
{
    public sealed class ReviewResult
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string? StudentPhoto { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
    }
}

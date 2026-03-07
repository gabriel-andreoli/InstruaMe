namespace InstruaMe.Domain.Models.Results
{
    public sealed class StudentProfileResult
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTimeOffset? Birthday { get; set; }
        public string? Photo { get; set; }
    }
}

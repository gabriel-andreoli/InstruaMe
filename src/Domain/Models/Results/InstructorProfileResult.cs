namespace InstruaMe.Domain.Models.Results
{
    public sealed class InstructorProfileResult
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public DateTimeOffset? Birthday { get; set; }
        public string? CarModel { get; set; }
        public string? Biography { get; set; }
        public string? Description { get; set; }
        public string? Photo { get; set; }
        public decimal PricePerHour { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public IReadOnlyList<ReviewResult> Reviews { get; set; }
    }
}

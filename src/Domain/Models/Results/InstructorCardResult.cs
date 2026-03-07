namespace InstruaMe.Domain.Models.Results
{
    public sealed class InstructorCardResult
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Photo { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? CarModel { get; set; }
        public decimal PricePerHour { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}

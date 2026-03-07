namespace InstruaMe.Domain.Models.Commands
{
    public sealed class UpdateInstructorCommand
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public DateTimeOffset? Birthday { get; set; }
        public string? CarModel { get; set; }
        public string? Biography { get; set; }
        public string? Description { get; set; }
        public string? Photo { get; set; }
        public decimal? PricePerHour { get; set; }
    }
}

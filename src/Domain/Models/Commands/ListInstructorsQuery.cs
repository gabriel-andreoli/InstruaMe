namespace InstruaMe.Domain.Models.Commands
{
    public sealed class ListInstructorsQuery
    {
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? CarModel { get; set; }
        public double? MinRating { get; set; }
        public decimal? MaxPricePerHour { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

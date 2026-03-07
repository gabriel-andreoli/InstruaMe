namespace InstruaMe.Domain.Models.Commands
{
    public sealed class SubmitReviewCommand
    {
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}

namespace InstruaMe.Domain.Models.Results
{
    public sealed class InstructorDashboardResult
    {
        public int TotalStudentReviewers { get; set; }
        public double AverageRating { get; set; }
        public IReadOnlyList<ReviewResult> RecentReviews { get; set; }
    }
}

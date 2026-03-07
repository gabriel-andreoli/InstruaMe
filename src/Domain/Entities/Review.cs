using InstruaMe.Domain.Entities.Base;

namespace InstruaMe.Domain.Entities
{
    public sealed class Review : EntityBase
    {
        public Guid InstructorId { get; set; }
        public Guid StudentId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }

        public Instructor Instructor { get; set; }
        public Student Student { get; set; }

        public Review() { }

        public Review(Guid instructorId, Guid studentId, int rating, string comment)
        {
            InstructorId = instructorId;
            StudentId = studentId;
            Rating = rating;
            Comment = comment;
            Register();
        }
    }
}

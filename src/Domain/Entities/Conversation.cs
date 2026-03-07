using InstruaMe.Domain.Entities.Base;

namespace InstruaMe.Domain.Entities
{
    public sealed class Conversation : EntityBase
    {
        public Guid InstructorId { get; set; }
        public Guid StudentId { get; set; }

        public Instructor Instructor { get; set; }
        public Student Student { get; set; }
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public Conversation() { }

        public Conversation(Guid instructorId, Guid studentId)
        {
            InstructorId = instructorId;
            StudentId = studentId;
            Register();
        }
    }
}

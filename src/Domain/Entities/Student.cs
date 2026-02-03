using InstruaMe.Domain.Entities.Base;
using InstruaMe.Domain.Models.Commands;
using InstruaMe.Domain.Models.Enums;

namespace InstruaMe.Domain.Entities
{
    public sealed class Student : EntityBase
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTimeOffset? Birthday { get; set; }
        public string Photo { get; set; }
        public string PasswordHash { get; private set; }
        public string PasswordSalt { get; private set; }
        public EUserRole Role { get; set; } = EUserRole.Student;

        public Student(RegisterStudentCommand command, string passwordHash, string passwordSalt)
        {
            Name = command.Name;
            Email = command.Email;
            Birthday = command.Birthday;
            Photo = command.Photo;

            PasswordHash = passwordHash;
            PasswordSalt = passwordSalt;

            Register();
        }
    }
}

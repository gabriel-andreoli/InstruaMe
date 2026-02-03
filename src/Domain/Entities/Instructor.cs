using InstruaMe.Domain.Entities.Base;
using InstruaMe.Domain.Models.Commands;
using InstruaMe.Domain.Models.Enums;

namespace InstruaMe.Domain.Entities
{
    public sealed class Instructor : EntityBase
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Document { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public DateTimeOffset? Birthday { get; set; }
        public string CarModel { get; set; }
        public string Biography { get; set; }
        public string Description { get; set; }
        public string PasswordHash { get; private set; }
        public string PasswordSalt { get; private set; }
        public EUserRole Role { get; set; } = EUserRole.Instructor;

        public Instructor() { }

        public Instructor(RegisterInstructorCommand command, string passwordHash, string passwordSalt)
        {
            Name = command.Name;
            Email = command.Email;
            Document = command.Document;

            State = command.State;
            City = command.City;
            Birthday = command.Birthday;

            CarModel = command.CarModel;
            Biography = command.Biography;
            Description = command.Description;

            PasswordHash = passwordHash;
            PasswordSalt = passwordSalt;

            Register();
        }
    }
}

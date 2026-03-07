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
        public string? Photo { get; set; }
        public decimal PricePerHour { get; set; }
        public string PasswordHash { get; private set; }
        public string PasswordSalt { get; private set; }
        public EUserRole Role { get; set; } = EUserRole.Instructor;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

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
            Photo = command.Photo;
            PricePerHour = command.PricePerHour;

            PasswordHash = passwordHash;
            PasswordSalt = passwordSalt;

            Register();
        }

        public void Update(UpdateInstructorCommand command)
        {
            if (command.Name is not null) Name = command.Name;
            if (command.PhoneNumber is not null) PhoneNumber = command.PhoneNumber;
            if (command.State is not null) State = command.State;
            if (command.City is not null) City = command.City;
            if (command.Birthday.HasValue) Birthday = command.Birthday;
            if (command.CarModel is not null) CarModel = command.CarModel;
            if (command.Biography is not null) Biography = command.Biography;
            if (command.Description is not null) Description = command.Description;
            if (command.Photo is not null) Photo = command.Photo;
            if (command.PricePerHour.HasValue) PricePerHour = command.PricePerHour.Value;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}

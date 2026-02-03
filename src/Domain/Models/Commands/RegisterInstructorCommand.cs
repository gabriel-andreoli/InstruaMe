namespace InstruaMe.Domain.Models.Commands
{
    public sealed class RegisterInstructorCommand
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

        public string Password { get; set; }
    }
}

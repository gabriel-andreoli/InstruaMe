namespace InstruaMe.Domain.Models.Commands
{
    public sealed class RegisterStudentCommand
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTimeOffset? Birthday { get; set; }
        public string Photo { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}

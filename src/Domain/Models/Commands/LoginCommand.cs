namespace InstruaMe.Domain.Models.Commands
{
    public sealed class LoginCommand
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}

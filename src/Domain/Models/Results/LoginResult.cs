using InstruaMe.Domain.Models.Enums;

namespace InstruaMe.Domain.Models.Results
{
    public sealed class LoginResult
    {
        public string Token { get; set; }
        public EUserRole Role { get; set; }
    }
}

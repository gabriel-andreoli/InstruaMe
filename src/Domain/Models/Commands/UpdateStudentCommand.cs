namespace InstruaMe.Domain.Models.Commands
{
    public sealed class UpdateStudentCommand
    {
        public string? Name { get; set; }
        public DateTimeOffset? Birthday { get; set; }
        public string? Photo { get; set; }
    }
}

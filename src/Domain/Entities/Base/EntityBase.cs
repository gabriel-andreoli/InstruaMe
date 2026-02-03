namespace InstruaMe.Domain.Entities.Base
{
    public abstract class EntityBase
    {
        public Guid Id { get; set; }
        public bool Deleted { get; set; }
        public DateTimeOffset? CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        protected void Register() 
        {
            Id = Guid.NewGuid();
        }
    }
}

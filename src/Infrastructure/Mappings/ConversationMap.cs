using InstruaMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstruaMe.Infrastructure.Mappings
{
    public class ConversationMap : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.ToTable(nameof(Conversation));

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.InstructorId, x.StudentId }).IsUnique();

            builder.HasOne(x => x.Instructor)
                .WithMany()
                .HasForeignKey(x => x.InstructorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Messages)
                .WithOne(x => x.Conversation)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

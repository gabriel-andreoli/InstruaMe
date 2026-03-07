using InstruaMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstruaMe.Infrastructure.Mappings
{
    public class ReviewMap : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable(nameof(Review));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Comment).HasMaxLength(2000);

            builder.HasIndex(x => new { x.InstructorId, x.StudentId }).IsUnique();

            builder.HasOne(x => x.Instructor)
                .WithMany(x => x.Reviews)
                .HasForeignKey(x => x.InstructorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

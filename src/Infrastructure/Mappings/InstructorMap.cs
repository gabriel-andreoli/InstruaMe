using InstruaMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstruaMe.Infrastructure.Mappings
{
    public class InstructorMap : IEntityTypeConfiguration<Instructor>
    {
        public void Configure(EntityTypeBuilder<Instructor> builder)
        {
            builder.ToTable(nameof(Instructor));

            builder.HasKey(x => x.Id);            
        }
    }
}

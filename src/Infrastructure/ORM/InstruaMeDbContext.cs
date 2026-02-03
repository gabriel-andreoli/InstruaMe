using InstruaMe.Domain.Entities;
using InstruaMe.Infrastructure.Mappings;
using Microsoft.EntityFrameworkCore;

namespace InstruaMe.Infrastructure.ORM
{
    public class InstruaMeDbContext : DbContext
    {
        public InstruaMeDbContext(DbContextOptions<InstruaMeDbContext> options) : base (options)
        {
            
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Instructor> Instructors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(InstruaMeDbContext).Assembly);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudentMap).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(InstructorMap).Assembly);
        }
    }
}

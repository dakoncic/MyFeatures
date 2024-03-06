using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL
{
    public class AzureCICDDbContext : DbContext
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AzureCICDDbContext(DbContextOptions<AzureCICDDbContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Model configuration goes here
        }
    }
}

using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL
{
    public class MyFeaturesDbContext : DbContext
    {
        public MyFeaturesDbContext(DbContextOptions<MyFeaturesDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }
        public DbSet<CommittedItem> CommittedItems { get; set; }

        //znači data annotations radimo za jednostavnije stvari direktno u entity klasi
        //a ovdje kompliciranije (fluent API) koje se tamo ne mogu, npr. veze, cascade delete itd.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // veza između Itema i CommitedItema
            // trebao biti eksplicitno postavljeno, iako već imamo data annotations u entity klasama
            modelBuilder.Entity<Item>()
                .HasMany<CommittedItem>()
                .WithOne(ci => ci.Item)
                .HasForeignKey(ci => ci.ItemId)
                .OnDelete(DeleteBehavior.Cascade); // kaskadno deletamo

            // indexi
            modelBuilder.Entity<CommittedItem>()
                .HasIndex(i => i.DueDate)
                .HasDatabaseName("IDX_DueDate"); //opcionalno al daje ime indexu

            modelBuilder.Entity<CommittedItem>()
                .HasIndex(ci => ci.ItemId)
                .HasDatabaseName("IDX_ItemID");

            modelBuilder.Entity<CommittedItem>()
                .HasIndex(ci => ci.CommittedDate)
                .HasDatabaseName("IDX_CommittedDate");

            modelBuilder.Entity<CommittedItem>()
                .HasIndex(ci => ci.CompletionDate)
                .HasDatabaseName("IDX_CompletionDate")
                .HasFilter("CompletionDate IS NOT NULL");  // uvjetni index, ako puno redaka ima NULL, da njih ne gleda

        }
    }
}

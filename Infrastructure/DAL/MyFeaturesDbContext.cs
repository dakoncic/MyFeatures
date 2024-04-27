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
        public DbSet<ItemTask> ItemTasks { get; set; }

        //znači data annotations radimo za jednostavnije stvari direktno u entity klasi
        //a ovdje kompliciranije (fluent API) koje se tamo ne mogu, npr. veze, cascade delete itd.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // veza između Itema i ItemTaska
            // trebao biti eksplicitno postavljeno
            modelBuilder.Entity<ItemTask>()
                .HasOne(it => it.Item)
                .WithMany(i => i.ItemTasks)
                .HasForeignKey(it => it.ItemId)
                .OnDelete(DeleteBehavior.Cascade); // kaskadno deletamo


            // indexi
            modelBuilder.Entity<ItemTask>()
                .HasIndex(i => i.DueDate)
                .HasDatabaseName("IDX_DueDate"); //opcionalno al daje ime indexu

            modelBuilder.Entity<ItemTask>()
                .HasIndex(ci => ci.ItemId)
                .HasDatabaseName("IDX_ItemID");

            modelBuilder.Entity<ItemTask>()
                .HasIndex(ci => ci.CommittedDate)
                .HasDatabaseName("IDX_CommittedDate");

            modelBuilder.Entity<ItemTask>()
                .HasIndex(ci => ci.CompletionDate)
                .HasDatabaseName("IDX_CompletionDate")
                .HasFilter("CompletionDate IS NOT NULL");  // uvjetni index, ako puno redaka ima NULL, da njih ne gleda

        }
    }
}

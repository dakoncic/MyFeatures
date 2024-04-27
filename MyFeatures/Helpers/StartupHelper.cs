using Core.DomainModels;
using Infrastructure.DAL;
using Mapster;
using Microsoft.EntityFrameworkCore;
using MyFeatures.DTO;
using Entity = Infrastructure.Entities;

namespace Infrastructure.Helpers
{
    public static class StartupHelper
    {
        public static void ApplyMigrations(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<MyFeaturesDbContext>(); // Replace with your actual DbContext
                    context.Database.Migrate(); // This applies any pending migrations
                }
                catch (Exception ex)
                {
                    //should replace with serilog
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while applying the database migrations.");
                }
            }
        }

        public static void ConfigureMapster()
        {
            //navodno registracija mapiranja za jednostavan property u property
            //nije potrebno

            // Configure mapping from DTOs to domain models and back
            TypeAdapterConfig<ItemDto, Item>.NewConfig()
                .TwoWays();

            // Configure mapping from domain models to entity models (if needed)
            TypeAdapterConfig<Item, Entity.Item>.NewConfig()
                .TwoWays();

            TypeAdapterConfig<ItemTask, Entity.ItemTask>.NewConfig()
                .TwoWays();

        }
    }
}

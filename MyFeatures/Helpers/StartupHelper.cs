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
            //mapiranja konfigurirati smjer svaki za sebem
            //.TwoWays() ne radi sa PreserveReference()


            //za sad ništa ne puca ako su zakomentirani
            //TypeAdapterConfig<Item, Entity.Item>.NewConfig()
            //    .PreserveReference(true);

            //TypeAdapterConfig<Entity.Item, Item>.NewConfig()
            //    .PreserveReference(true);

            //TypeAdapterConfig<ItemTask, Entity.ItemTask>.NewConfig()
            //    .PreserveReference(true);

            //za sad samo ovaj puca kod mapiranja u CreateItem, bez ovog dobim 'Access Violation'
            //cirkularna referenca
            TypeAdapterConfig<Entity.ItemTask, ItemTask>.NewConfig()
                .PreserveReference(true);

            TypeAdapterConfig<ItemTask, ItemTaskDto>.NewConfig()
                .PreserveReference(true);

        }
    }
}

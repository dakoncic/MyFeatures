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
                    var context = services.GetRequiredService<MyFeaturesDbContext>();
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
            //mapiranja konfigurirati smjer svaki za sebe
            //.TwoWays() ne radi sa PreserveReference()

            //za sad samo ovi pucaju kod mapiranja u CreateItem, bez ovog dobim 'Access Violation'
            //cirkularna referenca
            TypeAdapterConfig<Entity.ItemTask, ItemTask>.NewConfig()
                .PreserveReference(true);

            TypeAdapterConfig<ItemTask, ItemTaskDto>.NewConfig()
                .PreserveReference(true);
        }
    }
}

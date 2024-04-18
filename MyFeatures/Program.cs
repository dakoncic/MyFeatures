using Core.Interfaces;
using Core.Services;
using Infrastructure.DAL;
using Infrastructure.Helpers;
using Infrastructure.Interfaces.IRepository;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using MyFeatures.Helpers;

var builder = WebApplication.CreateBuilder(args);
var _configuration = builder.Configuration;

// Add services to the container.

//zapisat da moram eksplicitno dodat referencu sa webapi na core, sa core na infrastructure kroz csproj
//inače nije mogao iz web apia prepoznat MyFeaturesDbContext

//zapisat da moram entity framework design instalirat u web api inače migracije ne rade

builder.Services.AddDbContext<MyFeaturesDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure();
    });
});

builder.Services.AddScoped(typeof(IGenericCrudService<,>), typeof(GenericCrudService<,>));
builder.Services.AddScoped<IItemRepository, ItemRepository>();

builder.Services.AddScoped<IItemService, ItemService>();

builder.Services.AddControllers();

//mapster registracija nakon servisa
StartupHelper.ConfigureMapster();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//ovo će generirat ime akcije controllera za frontend isto kao što je i za backend
//npr. this.userService.GetAll()
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<CustomOperationIdFilter>();
});


var allowedOrigins = builder.Configuration.GetSection("CorsOrigins:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", corsBuilder =>
    {
        corsBuilder.WithOrigins(allowedOrigins)
                   .AllowAnyMethod()
                   .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//dodano zbog UI 
app.UseStaticFiles();

app.UseRouting();

app.UseCors("CorsPolicy");

app.MapControllers();
//dodano zbog UI
app.MapFallbackToFile("index.html");

StartupHelper.ApplyMigrations(app);

app.Run();

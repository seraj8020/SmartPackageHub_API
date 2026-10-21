using Autofac;
using Autofac.Extensions.DependencyInjection;
using SmartPackageHub.Common.Extension;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartPackageHub.Common.Types;
using SmartPackageHub.HubManagement.Infrastructure;
using BankingApp.AccountManagement.Infrastructure.AutofacModules;
using SmartPackageHub.HubManagement.Infrastructure.AutofacModules;
using HubManagement.Application.Intake;
using SmartPackageHub.HubManagement.Api.Intake;
using SmartPackageHub.HubManagement.Api;
using HubManagement.Api.SeedDataModel;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
// Add services to the container.
builder.AddHostLogging();
services.AddWebHostInfrastructure(builder.Configuration, "HubManagementService");
services.AddApiVersioning(ApiVersions.V2);
services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

var headers = new[] { "X-Operation", "X-Resource", "X-Total-Count" };
services.AddEndpointsApiExplorer();
services.AddRedis(builder.Configuration);
services.AddHttpContextAccessor();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddAuthorization();
var connectionString = builder.Configuration["DbContextSettings:ConnectionString"];
services.AddDbContext<HubDbContext>(opts => { opts.UseNpgsql(connectionString); });
services.AddSingleton<IntakeNotificationChannel>();
services.AddSingleton<IParcelPhotoStore, LocalBlobPhotoStore>();
services.AddHostedService<IntakeNotificationWorker>();

services.AddSwaggerDocs();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))
#if (UseSqlServer)
    .AddSqlServer(builder.Configuration["Database:SqlConnectionString"]!);
#else
    .AddNpgSql(connectionString);
#endif

var corsOriginUrl = builder.Configuration["CorsOrigin"] ?? throw new InvalidOperationException("CorsOrigin section was not found");
services
   .AddCors(options =>
   {
       options.AddPolicy("AllowOrigin",
                    builder => builder.WithOrigins(corsOriginUrl) //url here need to change from http to https if we are doing ssl communication
                             .AllowAnyHeader()
                             .AllowAnyMethod()
                         .AllowCredentials()
                         .WithExposedHeaders(headers));
   });

services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory()).ConfigureContainer<ContainerBuilder>((hostContext, container) =>
{
    container.RegisterModule(new ApplicationModule());
    container.RegisterModule(new InfrastructureModule(builder.Configuration));
});
var app = builder.Build();
string seedDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData");
if (!Directory.Exists(seedDataDirectory))
{
    seedDataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "SeedData");
}

var  seedFileTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
{
    ["Societies.json"] = typeof(SocietySeed),
    ["Users.json"] = typeof(UserSeed),
    ["UserSocieties.json"] = typeof(UserSocietySeed),
    ["Residents.json"] = typeof(ResidentSeed),
    ["Memberships.json"] = typeof(MembershipSeed),
    ["Shelves.json"] = typeof(ShelfSeed),
    ["CourierAgents.json"] = typeof(CourierAgentSeed),
    ["SecureBags.json"] = typeof(SecureBagSeed),
    ["DigitalSignOffReceipts.json"] = typeof(DigitalSignOffReceiptSeed),
    ["Packages.json"] = typeof(PackageSeed),
    ["PackageIntakes.json"] = typeof(PackageIntakeSeed),
    ["PackageImages.json"] = typeof(PackageImageSeed),
    ["AnalyticsDailies.json"] = typeof(AnalyticsDailySeed),
    ["Deliveries.json"] = typeof(DeliverySeed)
};


try
{
    app.Seed(JsonSeedDataLoader.LoadMultipleSeedData(seedDataDirectory, seedFileTypes));
}
catch (Exception ex)
{
    Console.WriteLine($"Seed data was not applied: {ex.Message}");
    if (args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase))
        throw;
}

if (args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase))
    return;


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

}
app.UseSwaggerDocs();

app.UseCors(options =>
{
    options.AllowAnyMethod()
           .AllowAnyHeader()
           .AllowAnyOrigin()
           .WithExposedHeaders("Content-Disposition");
});
//This is Required when we are doing ssl communication
app.UseRouting();
app.UseAuthorization();
app.MapHealthChecks("healthz");
app.MapHealthChecks("liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("self", StringComparison.Ordinal)
});
app.MapControllers();
await app.RunAsync().ConfigureAwait(false);

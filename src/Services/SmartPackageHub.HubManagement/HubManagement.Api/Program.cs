using Autofac;
using Autofac.Extensions.DependencyInjection;
using SmartPackageHub.Common.Extension;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartPackageHub.Common.Types;
using SmartPackageHub.HubManagement.Infrastructure;
using BankingApp.AccountManagement.Infrastructure.AutofacModules;
using SmartPackageHub.HubManagement.Infrastructure.AutofacModules;
using System.Threading.RateLimiting;
using SmartPackageHub.HubManagement.Api;
using SmartPackageHub.Common.Services.FeatureFlags.Abstractions;
using SmartPackageHub.Common.Services.FeatureFlags.Options;
using SmartPackageHub.Common.Services.FeatureFlags.Services;
using HubManagement.Workers;
using HubManagement.Api.SeedDataModel;
using System.IO.Compression;
using HubManagement.Application.Intake.Services;

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
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json", "application/problem+json"]);
});
services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

var headers = new[] { "X-Operation", "X-Resource", "X-Total-Count" };
services.AddEndpointsApiExplorer();
services.AddRedis(builder.Configuration);
services.AddMemoryCache();
services.AddHttpContextAccessor();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddAuthorization();
var connectionString = builder.Configuration["DbContextSettings:ConnectionString"];
services.AddDbContextPool<HubDbContext>(opts =>
{
    opts.UseNpgsql(connectionString);
    if (builder.Environment.IsDevelopment())
    {
       // opts.EnableSensitiveDataLogging();
        opts.LogTo(Console.WriteLine, LogLevel.Information);
    }
});
services.AddSingleton<IntakeNotificationChannel>();
services.AddLocalParcelPhotoStore();
services.AddOutboundMessaging(builder.Configuration);
services.AddHubManagementWorkers();
services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("otp-sms", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                // Local/dev MFA + resident OTP testing shares this bucket per IP.
                PermitLimit = 40,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
    options.AddPolicy("sms-webhook", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
services.Configure<FeatureFlagsOptions>(builder.Configuration.GetSection(FeatureFlagsOptions.SectionName));
services.Configure<HubManagement.Application.Auth.Options.AuthOptions>(
    builder.Configuration.GetSection(HubManagement.Application.Auth.Options.AuthOptions.SectionName));
services.AddSingleton<IFeatureFlags, FeatureFlagsService>();
services.AddSingleton<HubManagement.Application.Auth.Services.PlatformAdminSessionStore>();

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
    container.RegisterModule(new InfrastructureModule());
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
    ["Deliveries.json"] = typeof(DeliverySeed),
    ["DeliverySlots.json"] = typeof(DeliverySlotSeed)
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
app.UseResponseCompression();

app.UseCors(options =>
{
    options.AllowAnyMethod()
           .AllowAnyHeader()
           .AllowAnyOrigin()
           .WithExposedHeaders("Content-Disposition");
});
//This is Required when we are doing ssl communication
app.UseRouting();
app.UseRateLimiter();
app.UseAuthorization();
app.MapHealthChecks("healthz");
app.MapHealthChecks("liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("self", StringComparison.Ordinal)
});
app.MapControllers();
await app.RunAsync().ConfigureAwait(false);

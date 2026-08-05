using Microsoft.EntityFrameworkCore;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Models;
using SmartPackageHub_API.Services;
using System.Linq;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core - PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOtpService, OtpService>();

var app = builder.Build();

// Ensure database created for local development (use migrations in production)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Seed sample test data via separate seeder class (force to ensure data inserted)
    SeedData.EnsureSeedData(db, force: true);
}

// Configure the HTTP request pipeline.
// Enable Swagger UI for all environments so you can inspect available methods.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

using Microsoft.EntityFrameworkCore;
using MToGo.PartnerService.Entities;
using MToGo.PartnerService.Metrics;
using MToGo.PartnerService.Repositories;
using MToGo.PartnerService.Services;
using MToGo.Shared.Logging;
using MToGo.Shared.Metrics;
using MToGo.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo Security (JWT Authentication & Authorization)
builder.Services.AddMToGoSecurity(builder.Configuration);

// Add Prometheus metrics
builder.Services.AddMToGoMetrics();
builder.Services.AddHostedService<PartnerMetricsCollector>();

// Add DbContext
builder.Services.AddDbContext<PartnerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository and Service
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<IPartnerService, PartnerService>();

// Add Kafka logging for centralized log collection
builder.Services.AddKafkaLogging("PartnerService", LogLevel.Information);

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add HTTP metrics middleware
app.UseMToGoHttpMetrics();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map Prometheus metrics endpoint at /metrics
app.MapMToGoMetrics();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }

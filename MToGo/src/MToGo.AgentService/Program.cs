using Microsoft.EntityFrameworkCore;
using MToGo.AgentService.Entities;
using MToGo.AgentService.Metrics;
using MToGo.AgentService.Repositories;
using MToGo.AgentService.Services;
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
builder.Services.AddHostedService<AgentMetricsCollector>();

// Add DbContext
builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository and Service
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IAgentService, AgentService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
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

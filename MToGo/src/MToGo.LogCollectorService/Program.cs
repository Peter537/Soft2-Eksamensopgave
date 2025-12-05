using Microsoft.EntityFrameworkCore;
using MToGo.LogCollectorService.Entities;
using MToGo.LogCollectorService.Services;
using MToGo.Shared.Kafka;
using MToGo.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo Security (JWT Authentication & Authorization)
builder.Services.AddMToGoSecurity(builder.Configuration);

// Add database context
builder.Services.AddDbContext<LogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Kafka consumer
builder.Services.Configure<KafkaConsumerConfig>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<IKafkaConsumer, KafkaConsumer>();

// Add log services
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<ISystemLogService, SystemLogService>();

// Add hosted service for consuming logs
builder.Services.AddHostedService<LogConsumerService>();

var app = builder.Build();

// Auto-create database tables
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LogDbContext>();
    dbContext.Database.EnsureCreated();
}

// Ensure logs directory exists
var logsDir = app.Configuration["Logging:SystemLogsDirectory"] ?? "/var/log/mtogo";
Directory.CreateDirectory(logsDir);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

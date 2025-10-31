using NotificationService;
using NotificationService.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Add Kafka config
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
{
    ["Kafka:BootstrapServers"] = "localhost:9092"
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for Blazor app
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7066", "http://localhost:5066")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add repositories and services
builder.Services.AddSingleton<NotificationRepository>();

// Add background Kafka consumers
builder.Services.AddHostedService<OrderAcceptedConsumer>();
builder.Services.AddHostedService<OrderPreparingConsumer>();
builder.Services.AddHostedService<OrderReadyConsumer>();
builder.Services.AddHostedService<OrderPickedUpConsumer>();
builder.Services.AddHostedService<DriverArrivingConsumer>();
builder.Services.AddHostedService<OrderDeliveredConsumer>();

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

// Suppress Microsoft logs
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

// Startup banner
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(@"
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║           📢 NOTIFICATION SERVICE STARTING 📢              ║
║                                                            ║
║  🎯 Listening for Kafka events on all topics              ║
║  🌐 REST API: http://localhost:5230                        ║
║  📊 Swagger: http://localhost:5230/swagger                 ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
");
Console.ResetColor();

app.Run();

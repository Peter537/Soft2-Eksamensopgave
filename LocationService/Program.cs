using LocationService.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Add Kafka config (allow override from environment variables)
var kafkaBootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Kafka:BootstrapServers"] = kafkaBootstrapServers
});

// Add background Kafka consumer
builder.Services.AddHostedService<OrderPickedUpConsumer>();

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

var app = builder.Build();

// Startup banner
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(@"
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║            🗺️  LOCATION SERVICE STARTING 🗺️               ║
║                                                            ║
║  📍 GPS Tracking Simulation                                ║
║  🎯 Listening for order-pickedup events                    ║
║  📡 Publishing location updates every 5 seconds            ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
");
Console.ResetColor();

app.Run();

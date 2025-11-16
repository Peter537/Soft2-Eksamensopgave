using OrderService.Services;
using Shared.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application services
builder.Services.AddSingleton<OrderRepository>();

// Register Kafka producer
var kafkaBootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
builder.Services.AddSingleton(new KafkaProducerService(kafkaBootstrapServers));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.MapControllers();

// Startup message
Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
Console.WriteLine("║          📦 ORDERSERVICE - ORDER MANAGEMENT 📦         ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝");
Console.WriteLine($"📡 Kafka Connected: {kafkaBootstrapServers}");
Console.WriteLine("💾 Database: In-Memory (for demo purposes)");
Console.WriteLine("🚀 Ready to process orders...\n");

app.Run();

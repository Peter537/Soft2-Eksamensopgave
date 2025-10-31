using CentralHub.API.Services;
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

// Add CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("http://localhost:5198", "https://localhost:7198")
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

app.UseCors("AllowBlazor");
app.UseHttpsRedirection();
app.MapControllers();

// Startup message
Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
Console.WriteLine("║        🌟 CENTRALHUB.API - CUSTOMER GATEWAY 🌟        ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝");
Console.WriteLine($"📡 Kafka Connected: {kafkaBootstrapServers}");
Console.WriteLine("🚀 Ready to receive customer orders...\n");

app.Run();

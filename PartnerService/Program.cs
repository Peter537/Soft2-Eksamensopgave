using PartnerService.Services;
using PartnerService.BackgroundServices;
using Shared.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application services
builder.Services.AddSingleton<PartnerOrderRepository>();

// Register Kafka producer
var kafkaBootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
builder.Services.AddSingleton(new KafkaProducerService(kafkaBootstrapServers));

// Register background service for consuming OrderCreated events
builder.Services.AddHostedService<OrderCreatedConsumer>();

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
Console.WriteLine("║         🍕 PARTNERSERVICE - RESTAURANT GATEWAY 🍕      ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝");
Console.WriteLine($"📡 Kafka Connected: {kafkaBootstrapServers}");
Console.WriteLine("🚀 Listening for new orders from CentralHub...\n");

app.Run();

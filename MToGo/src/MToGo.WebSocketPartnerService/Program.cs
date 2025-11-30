using MToGo.Shared.Kafka;
using MToGo.WebSocketPartnerService.BackgroundServices;
using MToGo.WebSocketPartnerService.Handlers;
using MToGo.WebSocketPartnerService.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<PartnerConnectionManager>();
builder.Services.AddSingleton<PartnerWebSocketHandler>();

// Register Kafka producer for test endpoints
builder.Services.Configure<KafkaProducerConfig>(options =>
{
    options.BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
});
builder.Services.AddSingleton<KafkaProducer>();

// Register Kafka consumers as background services
builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddHostedService<AgentAssignedConsumer>();
builder.Services.AddHostedService<OrderPickedUpConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable WebSocket support
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.UseAuthorization();
app.MapControllers();

// WebSocket endpoint for partners: /{partnerId}
// Gateway routes /api/v1/ws/partners/{id} -> this service as /{id}
app.Map("/{partnerId:int}", async (HttpContext context, int partnerId, PartnerWebSocketHandler handler) =>
{
    await handler.HandleConnectionAsync(context, partnerId);
});

app.Run();

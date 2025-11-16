using WebsocketPartnerService.BackgroundServices;
using WebsocketPartnerService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register WebSocket connection manager (singleton to maintain connections)
builder.Services.AddSingleton<WebSocketConnectionManager>();

// Register Kafka consumer for OrderCreated events
var kafkaBootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
builder.Services.AddHostedService(sp => new OrderCreatedWebSocketConsumer(
    sp.GetRequiredService<WebSocketConnectionManager>(),
    kafkaBootstrapServers
));

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

// Enable WebSockets
app.UseWebSockets();

// WebSocket endpoint for restaurant partners
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionManager = context.RequestServices.GetRequiredService<WebSocketConnectionManager>();
        
        // Get restaurant ID from query string (in production, use auth token)
        var restaurantId = context.Request.Query["restaurantId"].ToString();
        if (string.IsNullOrEmpty(restaurantId))
        {
            restaurantId = "DEFAULT_RESTAURANT";
        }

        await connectionManager.HandleWebSocketConnection(restaurantId, webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.UseHttpsRedirection();
app.MapControllers();

// Startup message
Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
Console.WriteLine("║    🔌 WEBSOCKETPARTNERSERVICE - REALTIME UPDATES 🔌   ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝");
Console.WriteLine($"📡 Kafka Connected: {kafkaBootstrapServers}");
Console.WriteLine($"🌐 WebSocket Endpoint: ws://localhost:[port]/ws?restaurantId=RESTAURANT_ID");
Console.WriteLine("🚀 Ready to push real-time updates to restaurant screens...\n");
Console.WriteLine("╔════════════════════════════════════════════════════════╗");
Console.WriteLine("║  📝 DEVELOPER NOTES - WebSocket Implementation        ║");
Console.WriteLine("╠════════════════════════════════════════════════════════╣");
Console.WriteLine("║  This service demonstrates WebSocket architecture:     ║");
Console.WriteLine("║                                                        ║");
Console.WriteLine("║  1. Restaurants connect via: ws://host/ws             ║");
Console.WriteLine("║  2. Kafka events trigger WebSocket messages           ║");
Console.WriteLine("║  3. Restaurant screens update in real-time            ║");
Console.WriteLine("║                                                        ║");
Console.WriteLine("║  Current Status: EXAMPLE/PLACEHOLDER CODE             ║");
Console.WriteLine("║  - Uses standard .NET WebSockets (not SignalR)       ║");
Console.WriteLine("║  - Connection management is simplified                ║");
Console.WriteLine("║  - Production needs: authentication, reconnection     ║");
Console.WriteLine("║                      error handling, scaling          ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

app.Run();

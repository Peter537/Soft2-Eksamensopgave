using MToGo.WebSocketAgentService.BackgroundServices;
using MToGo.WebSocketAgentService.Handlers;
using MToGo.WebSocketAgentService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register our services
builder.Services.AddSingleton<AgentConnectionManager>();
builder.Services.AddSingleton<AgentWebSocketHandler>();

// Kafka consumers
builder.Services.AddHostedService<OrderAcceptedConsumer>();
builder.Services.AddHostedService<AgentAssignedConsumer>();
builder.Services.AddHostedService<OrderReadyConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.UseAuthorization();
app.MapControllers();

// WebSocket endpoints
// Gateway routes /api/v1/ws/agents -> this service as / (PathRemovePrefix)
// Gateway routes /api/v1/ws/agents/{id} -> this service as /{id}
var wsHandler = app.Services.GetRequiredService<AgentWebSocketHandler>();

// Broadcast room - all agents see available jobs
// Gateway: /api/v1/ws/agents -> / (after PathRemovePrefix)
app.Map("/", async context =>
{
    await wsHandler.HandleBroadcastConnectionAsync(context);
});

// Personal room - specific agent gets order updates
// Gateway: /api/v1/ws/agents/{id} -> /{id} (after PathRemovePrefix)
app.Map("/{agentId:int}", async (HttpContext context, int agentId) =>
{
    await wsHandler.HandleAgentConnectionAsync(context, agentId);
});

app.Run();


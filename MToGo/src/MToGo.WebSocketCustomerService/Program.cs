using MToGo.Shared.Security;
using MToGo.WebSocketCustomerService.BackgroundServices;
using MToGo.WebSocketCustomerService.Handlers;
using MToGo.WebSocketCustomerService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo Security with WebSocket support (token from query string)
builder.Services.AddMToGoSecurityWithWebSockets(builder.Configuration);

// Register WebSocket services
builder.Services.AddSingleton<CustomerConnectionManager>();
builder.Services.AddSingleton<CustomerWebSocketHandler>();

// Register Kafka consumers as hosted services
builder.Services.AddHostedService<OrderAcceptedConsumer>();
builder.Services.AddHostedService<OrderRejectedConsumer>();
builder.Services.AddHostedService<OrderPickedUpConsumer>();
builder.Services.AddHostedService<OrderDeliveredConsumer>();
builder.Services.AddHostedService<OrderReadyConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

// Map WebSocket endpoint for customers
// Gateway routes /api/v1/ws/customers/{id} -> /{id} after PathRemovePrefix
app.Map("/{id:int}", async (HttpContext context, int id) =>
{
    var handler = context.RequestServices.GetRequiredService<CustomerWebSocketHandler>();
    await handler.HandleConnectionAsync(context, id);
});

app.MapControllers();

app.Run();

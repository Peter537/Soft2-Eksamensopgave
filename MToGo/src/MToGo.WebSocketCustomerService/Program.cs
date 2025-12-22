using MToGo.Shared.Security;
using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Authorization;
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

// Enable WebSockets before authentication so JWT bearer can detect WS upgrade requests
// and read the token from the query string (access_token).
app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

// Map WebSocket endpoint for customers
// Gateway routes /api/v1/ws/customers/{id} -> /{id} after PathRemovePrefix
app.Map("/{id:int}", async (HttpContext context, int id) =>
{
    var idClaim = context.User.FindFirst(JwtClaims.Id)?.Value;
    if (!int.TryParse(idClaim, out var authenticatedCustomerId) || authenticatedCustomerId != id)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    var handler = context.RequestServices.GetRequiredService<CustomerWebSocketHandler>();
    await handler.HandleConnectionAsync(context, id);
}).RequireAuthorization(AuthorizationPolicies.CustomerOnly);

app.MapControllers();

app.Run();

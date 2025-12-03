using MToGo.Shared.Security;
using MToGo.Shared.Security.Cors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo CORS with trusted origins policy
// In production, origins are configured via CorsSettings in appsettings.json
// In development, fallback to localhost origins
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddMToGoCorsDevelopment();
}
else
{
    builder.Services.AddMToGoCors(builder.Configuration);
}

// Add MToGo Security with WebSocket support (for proxied WebSocket connections)
builder.Services.AddMToGoSecurityWithWebSockets(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
// Use the trusted origins policy for all requests
app.UseCors(CorsPolicies.TrustedOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");
app.MapReverseProxy();

app.Run();

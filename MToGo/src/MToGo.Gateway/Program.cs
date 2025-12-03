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

// Get CORS settings for logging middleware
var corsSettings = app.Services.GetRequiredService<CorsSettings>();

// Configure the HTTP request pipeline.
// Add CORS logging before CORS middleware to log blocked requests
app.UseCorsLogging(corsSettings);

// Use the trusted origins policy for all requests
app.UseCors(CorsPolicies.TrustedOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");
app.MapReverseProxy();

app.Run();

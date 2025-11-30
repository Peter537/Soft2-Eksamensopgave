using MToGo.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo Security with WebSocket support (for proxied WebSocket connections)
builder.Services.AddMToGoSecurityWithWebSockets(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");
app.MapReverseProxy();

app.Run();

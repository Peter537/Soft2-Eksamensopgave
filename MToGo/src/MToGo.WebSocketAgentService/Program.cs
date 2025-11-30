using MToGo.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo Security with WebSocket support (token from query string)
builder.Services.AddMToGoSecurityWithWebSockets(builder.Configuration);

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
app.MapControllers();

app.Run();

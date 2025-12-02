using MToGo.NotificationService.BackgroundServices;
using MToGo.NotificationService.Clients;
using MToGo.NotificationService.Services;
using MToGo.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Register services
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure HttpClient for Legacy API via Gateway
builder.Services.AddHttpClient<ILegacyNotificationApiClient, LegacyNotificationApiClient>(client =>
{
    var gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"] ?? "http://gateway:8080";
    client.BaseAddress = new Uri(gatewayBaseUrl);
});

// Register Kafka consumers as background services
builder.Services.AddHostedService<OrderAcceptedNotificationConsumer>();
builder.Services.AddHostedService<OrderRejectedNotificationConsumer>();
builder.Services.AddHostedService<OrderPickedUpNotificationConsumer>();
builder.Services.AddHostedService<OrderDeliveredNotificationConsumer>();

// Add MToGo Security (JWT Authentication & Authorization)
builder.Services.AddMToGoSecurity(builder.Configuration);

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

app.MapControllers();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }

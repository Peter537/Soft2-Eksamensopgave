using MToGo.OrderService.Entities;
using MToGo.OrderService.Repositories;
using MToGo.OrderService.Services;
using MToGo.Shared.Kafka;
using MToGo.Shared.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo Security (JWT Authentication & Authorization)
builder.Services.AddMToGoSecurity(builder.Configuration);

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddHttpClient<IPartnerServiceClient, PartnerServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:8080");
});

builder.Services.AddHttpClient<IAgentServiceClient, AgentServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:8080");
});

builder.Services.Configure<KafkaProducerConfig>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

var app = builder.Build();

// Auto-create database tables in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

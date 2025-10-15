using LegacyMToGoSystem.Infrastructure;
using LegacyMToGoSystem.Infrastructure.Messaging;
using LegacyMToGoSystem.Repositories;
using LegacyMToGoSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Legacy MToGo System API",
        Version = "v1",
        Description = "The system that is to be integrated into a modern service-based architecture"
    });
});

builder.Services.AddSingleton<IDatabase, JsonFileDatabase>();

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddScoped<IBusinessPartnerRepository, BusinessPartnerRepository>();
builder.Services.AddScoped<IBusinessPartnerService, BusinessPartnerService>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Services.AddSingleton<RabbitMQProducer>();
builder.Services.AddSingleton<KafkaProducer>();

builder.Services.AddHostedService<NotificationService>();

builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
    await database.InitializeAsync();
    
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Legacy MToGo System API v1");
        options.RoutePrefix = string.Empty;
    });
}

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

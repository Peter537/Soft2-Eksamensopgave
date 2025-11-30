using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Services;
using MToGo.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo Security (JWT Authentication & Authorization)
builder.Services.AddMToGoSecurity(builder.Configuration);

builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddHttpClient<ILegacyCustomerApiClient, LegacyCustomerApiClient>(client =>
{
    var gatewayUrl = builder.Configuration["Gateway:BaseUrl"]
        ?? throw new InvalidOperationException("Gateway:BaseUrl not configured");
    client.BaseAddress = new Uri(gatewayUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

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

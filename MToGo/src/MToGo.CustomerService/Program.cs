using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }

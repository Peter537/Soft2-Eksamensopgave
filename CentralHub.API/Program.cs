var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HttpClient for routing to backend services
builder.Services.AddHttpClient();

// Add CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("http://localhost:5198", "https://localhost:7198")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazor");
app.UseHttpsRedirection();
app.MapControllers();

// Startup message
Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
Console.WriteLine("║          🌟 CENTRALHUB - API GATEWAY 🌟               ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝");
Console.WriteLine("� Gateway Mode: Routes requests to backend services");
Console.WriteLine("� OrderService: http://localhost:5100");
Console.WriteLine("📍 PartnerService: http://localhost:5220");
Console.WriteLine("🚀 No business logic - pure routing layer\n");

app.Run();

using MToGo.Website.Components;
using MToGo.Website.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register services
builder.Services.AddScoped<CultureService>();

var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "http://localhost:8080";
builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri(gatewayUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gateway"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

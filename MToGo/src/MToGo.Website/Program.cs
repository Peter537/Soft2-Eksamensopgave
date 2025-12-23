using MToGo.Website.Components;
using MToGo.Website.Services;
using MToGo.Website.Services.Payment;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Trust reverse proxy forwarded headers (ingress-nginx) so HTTPS termination at the edge
// is correctly reflected as Request.Scheme=https inside the app.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.RequireHeaderSymmetry = false;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CultureService>();
builder.Services.AddSingleton<IResourceManagerFlyweightFactory, ResourceManagerFlyweightFactory>();
builder.Services.AddScoped<ILocalizerService, LocalizerService>();
builder.Services.AddScoped<CartService>();

// Register Payment Strategies (Strategy Pattern)
builder.Services.AddScoped<IPaymentStrategy, CreditCardPaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategy, PayPalPaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategy, MobilePayPaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategy, ApplePayPaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategy, GooglePayPaymentStrategy>();
builder.Services.AddScoped<PaymentContext>();

var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "http://localhost:8080";
builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri(gatewayUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gateway"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Default: enable HTTPS redirection. Ingress terminates TLS, but the app still should redirect
// when it receives a plain HTTP request (e.g., first-time visitors / clients without HSTS).
// Docker Compose explicitly disables this via HttpsSettings__EnableHttpsRedirection=false.
var enableHttpsRedirection = builder.Configuration.GetValue("HttpsSettings:EnableHttpsRedirection", true);
if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

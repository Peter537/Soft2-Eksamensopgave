using Microsoft.EntityFrameworkCore;
using MToGo.FeedbackHubService.Data;
using MToGo.FeedbackHubService.Repositories;
using MToGo.FeedbackHubService.Services;
using MToGo.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo Security (JWT Authentication & Authorization)
builder.Services.AddMToGoSecurity(builder.Configuration);

// Add Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FeedbackDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Repositories
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// Add Services
builder.Services.AddScoped<IReviewService, ReviewService>();

var app = builder.Build();

// Apply migrations and ensure database exists
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FeedbackDbContext>();
    dbContext.Database.EnsureCreated();
}

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

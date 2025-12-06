using Microsoft.EntityFrameworkCore;
using MToGo.ManagementService.Entities;
using MToGo.ManagementService.Repositories;
using MToGo.ManagementService.Services;
using MToGo.Shared.Security;
using MToGo.Shared.Security.Password;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Add MToGo Security (JWT Authentication & Authorization)
builder.Services.AddMToGoSecurity(builder.Configuration);

// Add DbContext
builder.Services.AddDbContext<ManagementDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository and Service
builder.Services.AddScoped<IManagementUserRepository, ManagementUserRepository>();
builder.Services.AddScoped<IManagementService, ManagementService>();

var app = builder.Build();

// Ensure database is created and seed management user
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ManagementDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    dbContext.Database.EnsureCreated();
    
    // Seed management user if not exists
    if (!dbContext.ManagementUsers.Any())
    {
        var username = configuration["Management:Username"] 
            ?? throw new InvalidOperationException("Management:Username environment variable is required");
        var password = configuration["Management:Password"] 
            ?? throw new InvalidOperationException("Management:Password environment variable is required");
        var name = configuration["Management:Name"] ?? "Management Admin";
        
        var managementUser = new ManagementUser
        {
            Username = username.ToLowerInvariant(),
            Password = passwordHasher.HashPassword(password),
            Name = name,
            IsActive = true
        };
        
        dbContext.ManagementUsers.Add(managementUser);
        dbContext.SaveChanges();
    }
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

// Make Program accessible for integration tests
public partial class Program { }

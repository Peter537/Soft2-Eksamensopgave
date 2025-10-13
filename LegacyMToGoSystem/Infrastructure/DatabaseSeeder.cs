using LegacyMToGoSystem.Models;

namespace LegacyMToGoSystem.Infrastructure;

public class DatabaseSeeder
{
    private readonly IDatabase _database;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IDatabase database, ILogger<DatabaseSeeder> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var existingCustomers = await _database.LoadAsync<List<Customer>>("customers");
        
        if (existingCustomers != null && existingCustomers.Any())
        {
            _logger.LogInformation("Database already contains customers. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding initial customer data...");

        var customers = new List<Customer>
        {
            new Customer
            {
                Id = 1,
                Email = "pean@outlook.dk",
                PasswordHash = HashPassword("Peter123!"),
                FirstName = "Peter",
                LastName = "Andersen",
                PhoneNumber = "+45 23 45 67 89",
                Address = "Nørrebrogade 45, 2200 København N",
                PreferredLanguage = "da",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
                IsDeleted = false,
                DeletedAt = null
            },
            new Customer
            {
                Id = 2,
                Email = "odo@yahoo.dk",
                PasswordHash = HashPassword("Oskar456!"),
                FirstName = "Oskar",
                LastName = "Olsen",
                PhoneNumber = "+45 98 76 54 32",
                Address = "Vesterbrogade 123, 1620 København V",
                PreferredLanguage = "da",
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Customer
            {
                Id = 3,
                Email = "Joe@gmail.com",
                PasswordHash = HashPassword("Yusef789!"),
                FirstName = "Yusef",
                LastName = "Khafaji",
                PhoneNumber = "+45 12 34 56 78",
                Address = "Amagerbrogade 78, 2300 København S",
                PreferredLanguage = "en",
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                IsDeleted = false,
                DeletedAt = null
            }
        };

        await _database.SaveAsync("customers", customers);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}

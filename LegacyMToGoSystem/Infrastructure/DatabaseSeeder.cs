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
        await SeedCustomersAsync();
        await SeedBusinessPartnersAsync();
    }

    private async Task SeedCustomersAsync()
    {
        var existingCustomers = await _database.LoadAsync<List<Customer>>("customers");
        
        if (existingCustomers != null && existingCustomers.Any())
        {
            _logger.LogInformation("Database already contains customers. Skipping customer seed.");
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

    private async Task SeedBusinessPartnersAsync()
    {
        var existingPartners = await _database.LoadAsync<List<BusinessPartner>>("businesspartners");
        
        if (existingPartners != null && existingPartners.Any())
        {
            _logger.LogInformation("Database already contains business partners. Skipping partner seed.");
            return;
        }

        _logger.LogInformation("Seeding initial business partner data...");

        var partners = new List<BusinessPartner>
        {
            new BusinessPartner
            {
                Id = 1,
                Name = "Pizza Palace",
                Address = "Store Kongensgade 12, 1264 København K",
                CuisineType = "Italian",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                MenuItems = new List<MenuItem>
                {
                    new MenuItem { Id = 1, Name = "Margherita Pizza", Description = "Classic tomato and mozzarella", Price = 89.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 2, Name = "Pepperoni Pizza", Description = "Spicy pepperoni and cheese", Price = 109.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 3, Name = "Vegetariana Pizza", Description = "Fresh vegetables and mozzarella", Price = 95.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 4, Name = "Bruschetta", Description = "Toasted bread with tomatoes and basil", Price = 45.00m, Category = "Appetizer", IsAvailable = true },
                    new MenuItem { Id = 5, Name = "Tiramisu", Description = "Classic Italian dessert", Price = 55.00m, Category = "Dessert", IsAvailable = true },
                    new MenuItem { Id = 6, Name = "Coca Cola 0.5L", Description = "Soft drink", Price = 25.00m, Category = "Beverage", IsAvailable = true },
                    new MenuItem { Id = 7, Name = "Sparkling Water 0.5L", Description = "Carbonated water", Price = 20.00m, Category = "Beverage", IsAvailable = true }
                }
            },
            new BusinessPartner
            {
                Id = 2,
                Name = "Sushi Station",
                Address = "Gothersgade 87, 1123 København K",
                CuisineType = "Japanese",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-8),
                MenuItems = new List<MenuItem>
                {
                    new MenuItem { Id = 1, Name = "Salmon Nigiri (8 pcs)", Description = "Fresh salmon nigiri sushi", Price = 95.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 2, Name = "Tuna Nigiri (8 pcs)", Description = "Fresh tuna nigiri sushi", Price = 105.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 3, Name = "California Roll (8 pcs)", Description = "Crab, avocado, cucumber", Price = 85.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 4, Name = "Spicy Tuna Roll (8 pcs)", Description = "Tuna with spicy mayo", Price = 95.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 5, Name = "Edamame", Description = "Steamed soybeans with sea salt", Price = 35.00m, Category = "Appetizer", IsAvailable = true },
                    new MenuItem { Id = 6, Name = "Miso Soup", Description = "Traditional Japanese soup", Price = 30.00m, Category = "Appetizer", IsAvailable = true },
                    new MenuItem { Id = 7, Name = "Green Tea", Description = "Hot Japanese green tea", Price = 20.00m, Category = "Beverage", IsAvailable = true },
                    new MenuItem { Id = 8, Name = "Ramune", Description = "Japanese soda", Price = 25.00m, Category = "Beverage", IsAvailable = true }
                }
            },
            new BusinessPartner
            {
                Id = 3,
                Name = "Burger House",
                Address = "Istedgade 45, 1650 København V",
                CuisineType = "American",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                MenuItems = new List<MenuItem>
                {
                    new MenuItem { Id = 1, Name = "Classic Cheeseburger", Description = "Beef patty with cheese, lettuce, tomato", Price = 89.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 2, Name = "Bacon Burger", Description = "Beef patty with bacon and cheese", Price = 99.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 3, Name = "Veggie Burger", Description = "Plant-based patty with vegetables", Price = 85.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 4, Name = "Chicken Burger", Description = "Crispy chicken with special sauce", Price = 89.00m, Category = "Main", IsAvailable = true },
                    new MenuItem { Id = 5, Name = "French Fries", Description = "Golden crispy fries", Price = 35.00m, Category = "Side", IsAvailable = true },
                    new MenuItem { Id = 6, Name = "Onion Rings", Description = "Crispy battered onion rings", Price = 40.00m, Category = "Side", IsAvailable = true },
                    new MenuItem { Id = 7, Name = "Milkshake", Description = "Vanilla, chocolate, or strawberry", Price = 45.00m, Category = "Beverage", IsAvailable = true },
                    new MenuItem { Id = 8, Name = "Iced Tea 0.5L", Description = "Refreshing iced tea", Price = 25.00m, Category = "Beverage", IsAvailable = true }
                }
            }
        };

        await _database.SaveAsync("businesspartners", partners);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}

using LegacyMToGoSystem.DTOs;
using LegacyMToGoSystem.Models;
using LegacyMToGoSystem.Repositories;

namespace LegacyMToGoSystem.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ICustomerRepository customerRepository, ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<CustomerResponseDto> RegisterAsync(CreateCustomerDto createDto)
    {
        _logger.LogInformation("Attempting to register customer with email: {Email}", createDto.Email);

        if (await _customerRepository.ExistsByEmailAsync(createDto.Email))
        {
            _logger.LogWarning("Registration failed: Email already exists - {Email}", createDto.Email);
            throw new InvalidOperationException($"A customer with email {createDto.Email} already exists");
        }


        var customer = new Customer
        {
            Email = createDto.Email,
            PasswordHash = HashPassword(createDto.Password),
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            PhoneNumber = createDto.PhoneNumber,
            Address = createDto.Address,
            PreferredLanguage = createDto.PreferredLanguage
        };

        var createdCustomer = await _customerRepository.CreateAsync(customer);
        
        _logger.LogInformation("Successfully registered customer: {CustomerId}", createdCustomer.Id);

        return MapToResponseDto(createdCustomer);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
    {
        _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

        var customer = await _customerRepository.GetByEmailAsync(loginDto.Email);

        if (customer == null)
        {
            _logger.LogWarning("Login failed: Customer not found - {Email}", loginDto.Email);
            return new LoginResponseDto
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        if (!VerifyPassword(loginDto.Password, customer.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password - {Email}", loginDto.Email);
            return new LoginResponseDto
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        _logger.LogInformation("Successful login for customer: {CustomerId}", customer.Id);

        return new LoginResponseDto
        {
            Success = true,
            Message = "Login successful",
            Customer = MapToResponseDto(customer)
        };
    }

    public async Task<CustomerResponseDto?> GetCustomerByIdAsync(int id)
    {
        _logger.LogInformation("Fetching customer: {CustomerId}", id);

        var customer = await _customerRepository.GetByIdAsync(id);
        
        return customer != null ? MapToResponseDto(customer) : null;
    }

    public async Task<IEnumerable<CustomerResponseDto>> GetAllCustomersAsync()
    {
        _logger.LogInformation("Fetching all customers");

        var customers = await _customerRepository.GetAllAsync();
        
        return customers.Select(MapToResponseDto);
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        _logger.LogInformation("GDPR soft-delete request for customer: {CustomerId}", id);

        var result = await _customerRepository.SoftDeleteAsync(id);
        
        if (result)
        {
            _logger.LogInformation("Successfully soft-deleted customer: {CustomerId}", id);
        }
        else
        {
            _logger.LogWarning("Soft-delete failed: Customer not found - {CustomerId}", id);
        }

        return result;
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, storedHash);
    }


    private static CustomerResponseDto MapToResponseDto(Customer customer)
    {
        return new CustomerResponseDto
        {
            Id = customer.Id,
            Email = customer.Email,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            PreferredLanguage = customer.PreferredLanguage,
            CreatedAt = customer.CreatedAt
        };
    }
}

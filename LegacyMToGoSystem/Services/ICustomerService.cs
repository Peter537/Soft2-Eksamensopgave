using LegacyMToGoSystem.DTOs;

namespace LegacyMToGoSystem.Services;

public interface ICustomerService
{
    Task<CustomerResponseDto> RegisterAsync(CreateCustomerDto createDto);
    Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
    Task<CustomerResponseDto?> GetCustomerByIdAsync(int id);
    Task<IEnumerable<CustomerResponseDto>> GetAllCustomersAsync();
    Task<bool> DeleteCustomerAsync(int id);
}

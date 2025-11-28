using MToGo.CustomerService.Models;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Clients;

public interface ILegacyCustomerApiClient
{
    Task<CreateCustomerResponse> CreateCustomerAsync(Customer request);
}

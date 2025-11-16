using Microsoft.Extensions.Configuration;

namespace KafkaOrderDemo.Services;

public class ApiOrderService
{
    private readonly HttpClient _httpClient;
    private readonly string _centralHubUrl;
    private readonly string _partnerServiceUrl;

    public event Action? OnOrdersChanged;

    public ApiOrderService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _centralHubUrl = configuration["ApiUrls:CentralHub"] ?? "http://localhost:5288";
        _partnerServiceUrl = configuration["ApiUrls:PartnerService"] ?? "http://localhost:5220";
    }

    public async Task<string> CreateOrder(string customerName, string deliveryAddress, List<string> items, decimal totalPrice)
    {
        Console.WriteLine($"\n🍕 Frontend: Creating order for {customerName}...");
        
        var request = new
        {
            customerName,
            deliveryAddress,
            items,
            totalPrice
        };

        var response = await _httpClient.PostAsJsonAsync($"{_centralHubUrl}/api/orders", request);
        response.EnsureSuccessStatusCode();

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        
        Console.WriteLine($"✅ Frontend: Order {order?.OrderId} created successfully!");
        
        OnOrdersChanged?.Invoke();
        
        return order?.OrderId ?? "UNKNOWN";
    }

    public async Task<List<Order>> GetAllOrders()
    {
        try
        {
            // Route through CentralHub gateway
            var response = await _httpClient.GetFromJsonAsync<List<Order>>($"{_centralHubUrl}/api/orders");
            return response ?? new List<Order>();
        }
        catch
        {
            return new List<Order>();
        }
    }

    public async Task<List<Order>> GetActiveOrders()
    {
        var allOrders = await GetAllOrders();
        // Include Pending orders so Kitchen can see them and accept/reject
        return allOrders.Where(o => o.Status != "Rejected" && o.Status != "Delivered").ToList();
    }

    public async Task<List<Order>> GetPendingOrders()
    {
        try
        {
            // Route through CentralHub gateway
            var response = await _httpClient.GetFromJsonAsync<List<Order>>($"{_centralHubUrl}/api/orders/pending");
            return response ?? new List<Order>();
        }
        catch
        {
            return new List<Order>();
        }
    }

    public async Task<List<Order>> GetPickedUpOrders()
    {
        var allOrders = await GetAllOrders();
        return allOrders.Where(o => o.Status == "PickedUp").ToList();
    }

    public async Task AcceptOrder(string orderId)
    {
        // Route through CentralHub gateway
        var response = await _httpClient.PostAsync($"{_centralHubUrl}/api/orders/{orderId}/accept", null);
        response.EnsureSuccessStatusCode();
        OnOrdersChanged?.Invoke();
    }

    public async Task RejectOrder(string orderId)
    {
        // Route through CentralHub gateway
        var response = await _httpClient.PostAsJsonAsync($"{_centralHubUrl}/api/orders/{orderId}/reject", "Restaurant is busy");
        response.EnsureSuccessStatusCode();
        OnOrdersChanged?.Invoke();
    }

    public async Task UpdateOrderStatus(string orderId, string newStatus)
    {
        // Route through CentralHub gateway
        string endpoint = newStatus switch
        {
            "Ready" => $"{_centralHubUrl}/api/orders/{orderId}/ready",
            "PickedUp" => $"{_centralHubUrl}/api/orders/{orderId}/pickup",
            "Delivered" => $"{_centralHubUrl}/api/orders/{orderId}/delivered",
            _ => throw new ArgumentException($"Invalid status: {newStatus}")
        };

        var response = await _httpClient.PostAsync(endpoint, null);
        response.EnsureSuccessStatusCode();
        OnOrdersChanged?.Invoke();
    }

    public class Order
    {
        public string OrderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string DeliveryAddress { get; set; } = "";
        public string OrderDetails { get; set; } = "";
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "New";
        public DateTime CreatedAt { get; set; }
    }

    private class OrderResponse
    {
        public string OrderId { get; set; } = "";
    }
}

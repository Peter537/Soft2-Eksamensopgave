using Microsoft.Extensions.DependencyInjection;
using MToGo.OrderService.Entities;
using MToGo.OrderService.Tests.Fixtures;

namespace MToGo.OrderService.Tests.Helpers
{
    public static class OrderTestHelper
    {
        public static async Task<int> CreateOrderWithStatus(SharedTestWebApplicationFactory factory, OrderStatus status, int? agentId = null)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var order = new Order
            {
                CustomerId = 1,
                PartnerId = 1,
                AgentId = agentId,
                DeliveryAddress = "Test Address 123",
                DeliveryFee = 29,
                ServiceFee = 6,
                TotalAmount = 135,
                Status = status,
                Items = new List<OrderItem>
                {
                    new OrderItem { FoodItemId = 1, Name = "Pizza", Quantity = 1, UnitPrice = 100 }
                }
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            return order.Id;
        }
    }
}

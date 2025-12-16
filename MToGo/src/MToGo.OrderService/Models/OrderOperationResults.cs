namespace MToGo.OrderService.Models;

public enum AssignAgentResult
{
    Success,
    OrderNotFound,
    InvalidStatus,
    AgentAlreadyAssigned
}

public enum PickupResult
{
    Success,
    OrderNotFound,
    InvalidStatus,
    NoAgentAssigned
}

public enum DeliveryResult
{
    Success,
    OrderNotFound,
    InvalidStatus,
    NoAgentAssigned
}

public class GetOrderDetailResult
{
    public bool Success { get; init; }
    public OrderDetailResponse? Order { get; init; }
    public GetOrderDetailError? Error { get; init; }
}

public enum GetOrderDetailError
{
    NotFound,
    Forbidden
}

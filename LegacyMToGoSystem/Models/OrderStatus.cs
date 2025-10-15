namespace LegacyMToGoSystem.Models;

public enum OrderStatus
{
    Placed,
    PaymentProcessing,
    Paid,
    Preparing,
    AgentAssigned,
    InTransit,
    Delivered,
    Cancelled
}

using Microsoft.Extensions.Logging;

namespace MToGo.PartnerService.Logging;

public static partial class LoggerMessages
{
    // Add Menu Item - Controller logs
    [LoggerMessage(Level = LogLevel.Information, Message = "Received AddMenuItem request for PartnerId: {PartnerId}")]
    public static partial void ReceivedAddMenuItemRequest(this ILogger logger, int partnerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "AddMenuItem completed: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void AddMenuItemCompleted(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AddMenuItem failed: PartnerId={PartnerId}, Reason={Reason}")]
    public static partial void AddMenuItemFailed(this ILogger logger, int partnerId, string reason);

    // Add Menu Item - Service logs
    [LoggerMessage(Level = LogLevel.Information, Message = "Adding menu item for PartnerId: {PartnerId}, Name: {Name}, Price: {Price}")]
    public static partial void AddingMenuItem(this ILogger logger, int partnerId, string name, decimal price);

    [LoggerMessage(Level = LogLevel.Information, Message = "Menu item added: PartnerId={PartnerId}, MenuItemId={MenuItemId}, Name={Name}")]
    public static partial void MenuItemAdded(this ILogger logger, int partnerId, int menuItemId, string name);

    // Update Menu Item - Controller logs
    [LoggerMessage(Level = LogLevel.Information, Message = "Received UpdateMenuItem request for PartnerId: {PartnerId}, MenuItemId: {MenuItemId}")]
    public static partial void ReceivedUpdateMenuItemRequest(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "UpdateMenuItem completed: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void UpdateMenuItemCompleted(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "UpdateMenuItem failed: PartnerId={PartnerId}, MenuItemId={MenuItemId}, Reason={Reason}")]
    public static partial void UpdateMenuItemFailed(this ILogger logger, int partnerId, int menuItemId, string reason);

    // Update Menu Item - Service logs
    [LoggerMessage(Level = LogLevel.Information, Message = "Updating menu item: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void UpdatingMenuItem(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Menu item updated: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void MenuItemUpdated(this ILogger logger, int partnerId, int menuItemId);

    // Delete Menu Item - Controller logs
    [LoggerMessage(Level = LogLevel.Information, Message = "Received DeleteMenuItem request for PartnerId: {PartnerId}, MenuItemId: {MenuItemId}")]
    public static partial void ReceivedDeleteMenuItemRequest(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "DeleteMenuItem completed: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void DeleteMenuItemCompleted(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DeleteMenuItem failed: PartnerId={PartnerId}, MenuItemId={MenuItemId}, Reason={Reason}")]
    public static partial void DeleteMenuItemFailed(this ILogger logger, int partnerId, int menuItemId, string reason);

    // Delete Menu Item - Service logs
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleting menu item: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void DeletingMenuItem(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Menu item deleted: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void MenuItemDeleted(this ILogger logger, int partnerId, int menuItemId);

    // Common validation errors
    [LoggerMessage(Level = LogLevel.Warning, Message = "Partner not found: PartnerId={PartnerId}")]
    public static partial void PartnerNotFound(this ILogger logger, int partnerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Menu item not found: MenuItemId={MenuItemId}")]
    public static partial void MenuItemNotFound(this ILogger logger, int menuItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Menu item does not belong to partner: MenuItemId={MenuItemId}, PartnerId={PartnerId}")]
    public static partial void MenuItemNotOwnedByPartner(this ILogger logger, int menuItemId, int partnerId);
}

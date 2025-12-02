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
    [LoggerMessage(Level = LogLevel.Information, Message = "Adding menu item for PartnerId: {PartnerId}")]
    public static partial void AddingMenuItem(this ILogger logger, int partnerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Menu item added: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void MenuItemAdded(this ILogger logger, int partnerId, int menuItemId);

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

    // Public browsing - Service logs
    [LoggerMessage(Level = LogLevel.Information, Message = "Getting all active partners")]
    public static partial void GettingAllActivePartners(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Active partners retrieved: Count={Count}")]
    public static partial void ActivePartnersRetrieved(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting partner menu: PartnerId={PartnerId}")]
    public static partial void GettingPartnerMenu(this ILogger logger, int partnerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Partner menu retrieved: PartnerId={PartnerId}, ItemCount={ItemCount}")]
    public static partial void PartnerMenuRetrieved(this ILogger logger, int partnerId, int itemCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting menu item: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void GettingMenuItem(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Menu item retrieved: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void MenuItemRetrieved(this ILogger logger, int partnerId, int menuItemId);

    // Public browsing - Controller logs
    [LoggerMessage(Level = LogLevel.Information, Message = "Received GetAllPartners request")]
    public static partial void ReceivedGetAllPartnersRequest(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "GetAllPartners completed: Count={Count}")]
    public static partial void GetAllPartnersCompleted(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Received GetPartnerMenu request: PartnerId={PartnerId}")]
    public static partial void ReceivedGetPartnerMenuRequest(this ILogger logger, int partnerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "GetPartnerMenu completed: PartnerId={PartnerId}")]
    public static partial void GetPartnerMenuCompleted(this ILogger logger, int partnerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "GetPartnerMenu failed: PartnerId={PartnerId}, Reason={Reason}")]
    public static partial void GetPartnerMenuFailed(this ILogger logger, int partnerId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Received GetMenuItem request: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void ReceivedGetMenuItemRequest(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "GetMenuItem completed: PartnerId={PartnerId}, MenuItemId={MenuItemId}")]
    public static partial void GetMenuItemCompleted(this ILogger logger, int partnerId, int menuItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "GetMenuItem failed: PartnerId={PartnerId}, MenuItemId={MenuItemId}, Reason={Reason}")]
    public static partial void GetMenuItemFailed(this ILogger logger, int partnerId, int menuItemId, string reason);
}

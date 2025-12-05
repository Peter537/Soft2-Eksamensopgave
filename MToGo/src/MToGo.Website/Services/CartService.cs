using MToGo.Website.Models;

namespace MToGo.Website.Services;

/// <summary>
/// Service for managing the shopping cart across page navigations.
/// This is a scoped service, so the cart persists within a browser session.
/// </summary>
public class CartService
{
    private int? _partnerId;
    private List<CartItem> _items = new();

    public int? PartnerId => _partnerId;
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Adds an item to the cart. If the item already exists, increases the quantity.
    /// </summary>
    public void AddItem(int partnerId, int foodItemId, string name, decimal unitPrice)
    {
        // If switching to a different partner, clear the cart
        if (_partnerId.HasValue && _partnerId != partnerId)
        {
            Clear();
        }

        _partnerId = partnerId;

        var existingItem = _items.FirstOrDefault(i => i.FoodItemId == foodItemId);
        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            _items.Add(new CartItem
            {
                FoodItemId = foodItemId,
                Name = name,
                Quantity = 1,
                UnitPrice = unitPrice
            });
        }
    }

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    public void RemoveItem(int foodItemId)
    {
        var item = _items.FirstOrDefault(i => i.FoodItemId == foodItemId);
        if (item != null)
        {
            _items.Remove(item);
        }

        if (_items.Count == 0)
        {
            _partnerId = null;
        }
    }

    /// <summary>
    /// Increases the quantity of an item.
    /// </summary>
    public void IncreaseQuantity(int foodItemId)
    {
        var item = _items.FirstOrDefault(i => i.FoodItemId == foodItemId);
        if (item != null)
        {
            item.Quantity++;
        }
    }

    /// <summary>
    /// Decreases the quantity of an item. If quantity reaches 0, removes the item.
    /// </summary>
    public void DecreaseQuantity(int foodItemId)
    {
        var item = _items.FirstOrDefault(i => i.FoodItemId == foodItemId);
        if (item != null)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;
            }
            else
            {
                RemoveItem(foodItemId);
            }
        }
    }

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        _partnerId = null;
    }

    /// <summary>
    /// Checks if the cart has any items.
    /// </summary>
    public bool HasItems => _items.Count > 0;

    /// <summary>
    /// Gets the total number of items in the cart.
    /// </summary>
    public int TotalItems => _items.Sum(i => i.Quantity);
}

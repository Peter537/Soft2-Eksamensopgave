namespace MToGo.Website.Models
{
    public class CartItem
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}

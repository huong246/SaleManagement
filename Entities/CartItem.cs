namespace SaleManagement.Entities;

public class CartItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public int Quantity { get; set; }
}
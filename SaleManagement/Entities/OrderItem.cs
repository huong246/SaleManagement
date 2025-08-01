using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;
[Table("OrderItems")]
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; }
    public Item? Item { get; set; }
    public Shop? Shop { get; set; }
    public Guid ShopId { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
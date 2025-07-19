using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;

[Table("CartItems")]
public class CartItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public int Quantity { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;

[Table("Items")]
public class Item
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Color{get;set;}
    public string? Size{get;set;}
    public int SaleCount { get; set; } = 0;
    public Guid ShopId { get; set; }
    public virtual Shop? Shop { get; set; }
    public virtual ICollection<ItemImage> ItemImages { get; set; }

    public Item()
    {
        ItemImages = new HashSet<ItemImage>();
    }
    
    public Guid? CategoryId { get; set; }
    public virtual Category? Category { get; set; }
    
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
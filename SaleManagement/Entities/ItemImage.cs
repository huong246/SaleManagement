using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;
[Table("ItemImages")]
public class ItemImage
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public virtual Item Item { get; set; }
    public string ImageUrl { get; set; }
    public bool IsPrimary { get; set; } = false;
}
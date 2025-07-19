using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;
[Table("UserViewHistories")]
public class UserViewHistory
{
    [Key]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ItemId { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    public virtual User User{ get; set; }
    public virtual Item Item { get; set; }
}
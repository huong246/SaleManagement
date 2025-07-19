using System.ComponentModel.DataAnnotations.Schema;
using SaleManagement.Entities.Enums;

namespace SaleManagement.Entities;
[Table("OrderHistories")]
public class OrderHistory
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public  virtual Order Order { get; set; }
    public DateTime CreatedDate { get; set; }
    public OrderStatus Status { get; set; } 
    public string? Note { get; set; } //xem don hang duoc giao toi dau

    public OrderHistory()
    {
        CreatedDate = DateTime.UtcNow;
    }
}
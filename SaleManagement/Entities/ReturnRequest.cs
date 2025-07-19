using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SaleManagement.Entities.Enums;

namespace SaleManagement.Entities;
[Table("ReturnRequests")]
public class ReturnRequest
{
    [Key]
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; }
    public string? Reason { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime RequestedAt { get; set; }
}
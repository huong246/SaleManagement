using System.ComponentModel.DataAnnotations.Schema;
using SaleManagement.Entities.Enums;

namespace SaleManagement.Entities;

[Table("SellerUpgradeRequests")]
public class SellerUpgradeRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public virtual User? User { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public SellerUpgradeRequest()
    {
        RequestedAt = DateTime.UtcNow;
        Status = RequestStatus.Pending;
    }
   
}
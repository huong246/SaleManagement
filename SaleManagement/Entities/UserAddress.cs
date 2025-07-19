using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;
[Table("UserAddress")]
public class UserAddress
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } //ten goi nho cho dia chi
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsDefault { get; set; } =  false;
    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; }
}
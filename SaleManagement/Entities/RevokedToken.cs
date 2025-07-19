using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;

[Table("RevokedTokens")]
public class RevokedToken
{
    [Key]
    public string Jti { get; set; }
    public DateTime ExpiryDate { get; set; }
}
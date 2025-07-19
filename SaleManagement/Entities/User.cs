using System.ComponentModel.DataAnnotations.Schema;
using SaleManagement.Entities.Enums;

namespace SaleManagement.Entities;

[Table("Users")]
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    
    public string? Fullname { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public DateTime? Birthday { get; set; }
    
    public string? Gender { get; set; } //male, female, other
    public decimal Balance { get; set; }
    
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public virtual  UserRole UserRoles  { get; set; } //mac dinh ban dau se la customer

    public virtual ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public User()
    {
        Balance = 0;
        UserRoles = UserRole.Customer;
    }
}
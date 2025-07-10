using SaleManagement.Entities.Enums;

namespace SaleManagement.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public decimal Balance { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public virtual  UserRole UserRoles  { get; set; } //mac dinh ban dau se la customer

    public User()
    {
        Balance = 0;
        UserRoles = UserRole.Customer;
    }
}
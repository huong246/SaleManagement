using SaleManagement.Entities.Enums;
using SaleManagement.Services;

namespace SaleManagement.Schemas
{
    public record CreateUserRequest(string Username, string Password, string? FullName, string? PhoneNumber, DateTime? Birthday, string? Gender);
    public record LoginUserRequest(string Username, string Password);
    public record LoginUserResult(
        LoginUserResultType ResultType, 
        string? AccessToken, 
        string? RefreshToken
    );
    
  
}
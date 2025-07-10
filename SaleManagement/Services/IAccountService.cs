using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum CreateUserResult
{
    Success,
    UsernameExist,
    DatabaseError
}

public enum LoginUserResultType
{
    Success,
    InvalidCredentials
}

public enum LogoutUserResultType
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
}
public record LoginUserResult(LoginUserResultType LonginUserResultType, string? AccessToken, string? RefreshToken);
public interface IAccountService
{
    Task<CreateUserResult> CreateUser(CreateUserRequest request);
    Task<LoginUserResult> LoginUser(LoginUserRequest request);
    Task<LogoutUserResultType> LogoutAsync();

}
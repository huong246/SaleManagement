namespace SaleManagement.Schemas;

public record UserProfileDto(Guid Id, string Username, string? FullName, string? PhoneNumber, DateTime? Birthday, string? Gender);

public record UpdateProfileRequest(string? FullName, string? PhoneNumber, DateTime? Birthday, string? Gender);
public record AddressDto(Guid Id, string Name, double Latitude, double Longitude, bool IsDefault);
public record CreateAddressRequest(string Name, double Latitude, double Longitude, bool IsDefault);
public record UpdateAddressRequest(Guid AddressId, string? Name, double Latitude, double Longitude, bool IsDefault);
public record DeleteAddressRequest(Guid AddressId);

public enum UpdateUserProfileResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
}

public enum UpdateAddressResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    AdressNotFound,
}

public enum DeleteAddressResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    AddressNotFound,
}
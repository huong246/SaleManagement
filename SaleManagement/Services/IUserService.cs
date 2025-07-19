using SaleManagement.Schemas;

namespace SaleManagement.Services;


public interface IUserService
{
    Task<UserProfileDto?> GetUserProfileAsync();
    Task<UpdateUserProfileResult> UpdateUserProfileAsync(UpdateProfileRequest request);
    Task<IEnumerable<AddressDto>> GetAddressesAsync();
    Task<AddressDto?> CreateAddressAsync(CreateAddressRequest request);
    Task<UpdateAddressResult> UpdateAddressAsync(UpdateAddressRequest request);
    Task<DeleteAddressResult> DeleteAddressAsync(DeleteAddressRequest request);
    
}
using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum CreateShopResult
{
    Success,
    AuthenticationError,
    UserHasAlreadyShop,
    ShopNameExist,
    UserNotExist,
    DatabaseError,
}
public interface IShopService
{
    Task<CreateShopResult> CreateShop(CreateShopRequest request);
    
}
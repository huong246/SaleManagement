using SaleManagement.Entities;

namespace SaleManagement.Services;

public interface IShippingService
{
    Task<decimal> CalculateFeeAsync(Shop shop, User user);
    Task<string> CreateShippingOrderAsync(Order order); 
}
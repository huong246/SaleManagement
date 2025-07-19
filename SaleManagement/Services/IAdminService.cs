namespace SaleManagement.Services;

public record UserAdminView(Guid Id, string Username, string Roldes, decimal Balance);

public record ShopAdminView(Guid Id, string Name, string OwnerUsername, bool IsActive);

public record DashboardStats(decimal TotalRevenue, int TotalOrder, int TotalUser, int TotalShop);
public  interface IAdminService
{
    Task<IEnumerable<UserAdminView>> GetAllUserAsync();
    Task<IEnumerable<ShopAdminView>> GetAllShopAsync();
    Task<DashboardStats> GetDashboardStatsAsync();
    
}
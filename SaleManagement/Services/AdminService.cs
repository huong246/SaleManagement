using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;

namespace SaleManagement.Services;

public class AdminService : IAdminService
{
    private readonly ApiDbContext _dbContext;
    public AdminService(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<IEnumerable<UserAdminView>> GetAllUserAsync()
    {
        return await _dbContext.Users
            .Select(u => new UserAdminView(u.Id, u.Username, u.UserRoles.ToString(), u.Balance))
            .ToListAsync();
    }

    public async Task<IEnumerable<ShopAdminView>> GetAllShopAsync()
    {
        return await _dbContext.Shops
            .Include(s => s.User) 
            .Select(s => new ShopAdminView(
                s.Id,
                s.Name,
                s.User != null ? s.User.Username : "N/A", 
                true 
            ))
            .ToListAsync();
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var totalRevenue = await _dbContext.Orders
            .Where(o => o.Status == Entities.Enums.OrderStatus.completed)
            .SumAsync(o => o.TotalAmount);

        var totalOrders = await _dbContext.Orders.CountAsync();
        var totalUsers = await _dbContext.Users.CountAsync();
        var totalShops = await _dbContext.Shops.CountAsync();

        return new DashboardStats(totalRevenue, totalOrders, totalUsers, totalShops);
    }
}
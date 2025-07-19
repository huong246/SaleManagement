using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;

namespace SaleManagement.Services;

public class SellerService : ISellerService
{
    private readonly ApiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public SellerService(ApiDbContext dbContext, IHttpContextAccessor ihttpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = ihttpContextAccessor;
    }
    public async Task<SellerDashboardStats> GetSellerDashboardStatsAsync()
    {
       var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
       if (!Guid.TryParse(userIdString, out var userId))
       {
           return new SellerDashboardStats(0, 0, 0);
       }
       var shop = await _dbContext.Shops.FirstOrDefaultAsync(s=>s.UserId == userId);
       if (shop == null)
       {
           return new SellerDashboardStats(0, 0, 0);
       }

       var orderItem = await _dbContext.OrderItems.Where(o => o.ShopId == o.ShopId && o.Order.Status == OrderStatus.completed).ToListAsync();
       if (orderItem == null)
       {
           return new SellerDashboardStats(0, 0, 0);
       }

       decimal revenue = 0;
       foreach (var OrderItem in orderItem)
       {
           revenue += OrderItem.Item.Price * OrderItem.Quantity;
       }

       var pendingOrders = await _dbContext.Orders.CountAsync(o =>
           o.OrderItems.Any(oi => oi.ShopId == shop.Id) && o.Status == OrderStatus.pending);
       var totalProducts = await _dbContext.Items.CountAsync(i=>i.ShopId == shop.Id);
       return new SellerDashboardStats(revenue, pendingOrders, totalProducts);

    }

    public async Task<IEnumerable<Order>?> GetShopOrderAsync()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Enumerable.Empty<Order>();
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Enumerable.Empty<Order>();
        var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (shop == null)
            return Enumerable.Empty<Order>();
        return await _dbContext.Orders.AsNoTracking().Where(o => o.OrderItems.Any(oi=>oi.ShopId== shop.Id)).Include(o=>o.OrderItems).ThenInclude(oi=>oi.Item).OrderByDescending(o=>o.OrderDate).ToListAsync();
        
        
    }
}
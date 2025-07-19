using SaleManagement.Entities;

namespace SaleManagement.Services;


    public record SellerDashboardStats(decimal TotalRevenue, int PendingOrder, int TotalProducts);
    public interface ISellerService
    {
        Task<SellerDashboardStats> GetSellerDashboardStatsAsync();
        Task<IEnumerable<Order>?> GetShopOrderAsync();
    }

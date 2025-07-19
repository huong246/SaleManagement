using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class RecommendationService : IRecommendationService
{
    private readonly ApiDbContext _dbContext;
    
    public RecommendationService(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Item>> GetSimilarItemAsync(GetSimilarItemsRequest request)
    {
        var originalItem = await _dbContext.Items.FindAsync(request.ItemId);
        if (originalItem == null)
        {
            return Enumerable.Empty<Item>();
        }

        return await _dbContext.Items.Where(i => i.CategoryId == originalItem.CategoryId && i.Id != request.ItemId)
            .OrderBy(i => Guid.NewGuid()).Take(request.count).ToListAsync();
        
    }

    public async Task<IEnumerable<Item>> GetRecommendationForUserAsync(GetRecommendationForUserRequest request)
    {
        var purchasedItemIds = await _dbContext.OrderItems.Where(oi => oi.Order.UserId == request.userId)
            .OrderByDescending(oi => oi.Order.OrderDate).Select(oi => oi.ItemId).ToListAsync();
        var viewedItemIds = await _dbContext.UserViewHistories.Where(vh => vh.UserId == request.userId)
            .OrderByDescending(vh => vh.ViewedAt).Select(vh => vh.ItemId).Take(request.count).ToListAsync();
        var historyItemIds = purchasedItemIds.Concat(viewedItemIds).Distinct().ToList();
        var relatedCategoryIds = await _dbContext.Items
            .Where(i => historyItemIds.Contains(i.Id) && i.CategoryId.HasValue).Select(i => i.CategoryId.Value)
            .Distinct().ToListAsync();
        return await _dbContext.Items
            .Where(i => i.CategoryId.HasValue && relatedCategoryIds.Contains(i.CategoryId.Value))
            .Where(i => !historyItemIds.Contains(i.Id)).OrderBy(i => Guid.NewGuid()).Take(relatedCategoryIds.Count)
            .ToListAsync();
    }
} 
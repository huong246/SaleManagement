using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public interface IRecommendationService
{
    Task<IEnumerable<Item>> GetSimilarItemAsync(GetSimilarItemsRequest request);
    Task<IEnumerable<Item>> GetRecommendationForUserAsync(GetRecommendationForUserRequest request);
}
using Microsoft.AspNetCore.SignalR;

namespace SaleManagement.Schemas;

public record GetSimilarItemsRequest(Guid ItemId, int count);

public record GetRecommendationForUserRequest(Guid userId, int count);

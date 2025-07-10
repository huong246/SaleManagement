namespace SaleManagement.Schemas;

public record CreateReviewRequest(int Rating, string Comment, Guid ItemId);
public record DeleteReviewRequest( Guid ReviewId);
public record UpdateReviewRequest( Guid ReviewId, int? Rating, string? Comment);
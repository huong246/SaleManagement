using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum CreateReviewResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    ItemNotFound,
    UserHasNotPurchasedItem,
    AlreadyReviewed,
    RatingNotInvalid,
}

public enum DeleteReviewResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    ReviewNotFound,
}

public enum UpdateReviewResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    ReviewNotFound,
    RatingNotInvalid,
}
public interface IReviewService
{
    Task<CreateReviewResult> CreateReview(CreateReviewRequest request);
    Task<DeleteReviewResult> DeleteReview(DeleteReviewRequest request);
    Task<UpdateReviewResult> UpdateReview(UpdateReviewRequest request);
}
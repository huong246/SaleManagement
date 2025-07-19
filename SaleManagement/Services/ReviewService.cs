using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class ReviewService : IReviewService
{
    private readonly ApiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReviewService(ApiDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<CreateReviewResult> CreateReview(CreateReviewRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(!Guid.TryParse(username, out var userId))
        {
            return CreateReviewResult.TokenInvalid;
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return CreateReviewResult.UserNotFound;
        }

        var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null)
        {
            return CreateReviewResult.ItemNotFound;
        }

        var hasPurchased = await _dbContext.Orders.AnyAsync(o =>
            o.UserId == userId && o.Status == OrderStatus.completed &&
            o.OrderItems.Any(oi => oi.ItemId == request.ItemId));
        if (!hasPurchased)
        {
            return CreateReviewResult.UserHasNotPurchasedItem;
        }

        var alreadyReviewes =
            await _dbContext.ReviewAndRatings.FirstOrDefaultAsync(r =>
                r.ItemId == request.ItemId && r.UserId == userId);
        if (alreadyReviewes != null)
        {
            return CreateReviewResult.AlreadyReviewed;
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            return CreateReviewResult.RatingNotInvalid;
        }
        var newReview = new ReviewAndRating()
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            Item = item,
            UserId = user.Id,
            User = user,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedReviewDate = DateTime.UtcNow,
        };
        _dbContext.ReviewAndRatings.Add(newReview);
        try
        {
            await _dbContext.SaveChangesAsync();
            return CreateReviewResult.Success;
        }
        catch (DbUpdateException)
        {
            return CreateReviewResult.DatabaseError;
        }
    }

    public async Task<DeleteReviewResult> DeleteReview(DeleteReviewRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return DeleteReviewResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return DeleteReviewResult.UserNotFound;
        }
        var review = await _dbContext.ReviewAndRatings.FirstOrDefaultAsync(r => r.Id == request.ReviewId && r.UserId == user.Id);
        if (review == null)
        {
            return DeleteReviewResult.ReviewNotFound;
        }
        
        _dbContext.ReviewAndRatings.Remove(review);
        try
        {
            await _dbContext.SaveChangesAsync();
            return DeleteReviewResult.Success;
        }
        catch (DbUpdateException)
        {
            return DeleteReviewResult.DatabaseError;
        }
    }

    public async Task<UpdateReviewResult> UpdateReview(UpdateReviewRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UpdateReviewResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return UpdateReviewResult.UserNotFound;
        }
        var review = await _dbContext.ReviewAndRatings.FirstOrDefaultAsync(r => r.Id == request.ReviewId && r.UserId == user.Id);
        if (review == null)
        {
            return UpdateReviewResult.ReviewNotFound;
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            return UpdateReviewResult.RatingNotInvalid;       
        }
        review.Rating = request.Rating ?? review.Rating;
        review.Comment = request.Comment ?? review.Comment;
        try
        {
            await _dbContext.SaveChangesAsync();
            return UpdateReviewResult.Success;
        }
        catch (DbUpdateException)
        {
            return UpdateReviewResult.DatabaseError;       
        }
    }
}
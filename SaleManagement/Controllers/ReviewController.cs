using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;
    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost("create_review")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CreateReview(CreateReviewRequest request)
    {
        var result = await _reviewService.CreateReview(request);
        return result switch
        {
            CreateReviewResult.Success => Ok("Review created successfully"),
            CreateReviewResult.TokenInvalid => Unauthorized("Token is invalid"),
            CreateReviewResult.UserNotFound => NotFound("User not found"),
            CreateReviewResult.ItemNotFound => NotFound("Item not found"),
            CreateReviewResult.AlreadyReviewed => BadRequest("Item has been reviewed"),
            CreateReviewResult.UserHasNotPurchasedItem => BadRequest("User has not purchased the item"),
            CreateReviewResult.RatingNotInvalid => BadRequest("Rating is not invalid"),
            _ => StatusCode(500, "An unexpected error occurred while creating the review")
        };
    }

    [HttpPost("delete_review")]
    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Admin)}")]
    public async Task<IActionResult> DeleteReview(DeleteReviewRequest request)
    {
        var result = await _reviewService.DeleteReview(request);
        return result switch
        {
            DeleteReviewResult.Success => Ok("Review deleted successfully"),
            DeleteReviewResult.TokenInvalid => Unauthorized("Token is invalid"),
            DeleteReviewResult.UserNotFound => NotFound("User not found"),
            DeleteReviewResult.ReviewNotFound => NotFound("Review not found"),
            _ => StatusCode(500, "An unexpected error occurred while deleting the review")
        };
    }

    [HttpPost("update_review")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> UpdateReview(UpdateReviewRequest request)
    {
        var result = await _reviewService.UpdateReview(request);
        return result switch
        {
            UpdateReviewResult.Success => Ok("Review updated successfully"),
            UpdateReviewResult.TokenInvalid => Unauthorized("Token is invalid"),
            UpdateReviewResult.UserNotFound => NotFound("User not found"),
            UpdateReviewResult.ReviewNotFound => NotFound("Review not found"),
            UpdateReviewResult.RatingNotInvalid => BadRequest("Rating is not invalid"),
            _ => StatusCode(500, "An unexpected error occurred while updating the review")
        };

    }
}
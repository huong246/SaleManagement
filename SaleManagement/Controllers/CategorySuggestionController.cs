using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;
[ApiController]
[Route("api/[controller]")]

public class CategorySuggestionController:ControllerBase
{
    private readonly ISuggestionService _categorySuggestionService;

    public CategorySuggestionController(ISuggestionService categorySuggestionService)
    {
        _categorySuggestionService = categorySuggestionService;
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> CreateSuggestion([FromBody] CreateCategorySuggestionRequest request)
    {
        var result = await _categorySuggestionService.CreateSuggestionAsync(request);
        return result switch
        {
            SuggestionResult.Success => Ok("Suggestion created successfully"),
            SuggestionResult.UserNotFound => NotFound("User not found"),
            SuggestionResult.TokenInvalid => BadRequest("Token in valid"),
            _ => StatusCode(500, "An unexpected error occurred while creating the suggestion")
        };
    }
    [HttpGet("pending")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> GetPending()
    {
        var suggestions = await _categorySuggestionService.GetPendingSuggestionsAsync();
        return Ok(suggestions);
    }
    
    [HttpPost("{id}/approve")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _categorySuggestionService.ApproveSuggestionAsync(id);
        return result switch
        {
            SuggestionResult.Success => Ok("Suggestion approved and category created."),
            SuggestionResult.NotFound => NotFound("Suggestion not found."),
            SuggestionResult.NotPending => BadRequest("This suggestion has already been reviewed."),
            SuggestionResult.AlreadyExists => Conflict("A category with this name already exists."),
            _ => StatusCode(500, "An error occurred.")
        };
    }
    [HttpPost("{id}/reject")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Reject(Guid id)
    {
        var result = await _categorySuggestionService.RejectSuggestionAsync(id);
        return result switch
        {
            SuggestionResult.Success => Ok("Suggestion rejected."),
            SuggestionResult.NotFound => NotFound("Suggestion not found."),
            SuggestionResult.NotPending => BadRequest("This suggestion has already been reviewed."),
            _ => StatusCode(500, "An error occurred.")
        };
    }
}
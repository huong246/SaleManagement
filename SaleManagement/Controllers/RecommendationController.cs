using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]

public class RecommendationController :ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    public RecommendationController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }
    
    [HttpGet("similar/{itemId}")]
    public async Task<IActionResult> GetSimilarItems(GetSimilarItemsRequest request)
    {
        var items = await _recommendationService.GetSimilarItemAsync(request);
        return Ok(items);
    }

  
    [HttpGet("foryou")]
    [Authorize]
    public async Task<IActionResult> GetForYouRecommendations(GetRecommendationForUserRequest request)
    {
        
        var items = await _recommendationService.GetRecommendationForUserAsync(request);
        return Ok(items);
    }
}
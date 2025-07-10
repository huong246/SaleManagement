using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShopController : ControllerBase
{
    private readonly IShopService _shopService;
    public ShopController(IShopService shopService)
    {
        _shopService = shopService;
    }

    [HttpPost("create_shop")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> CreateShop([FromBody] CreateShopRequest request)
    {
        var result = await _shopService.CreateShop(request);
        return result switch
        {
            CreateShopResult.Success => Ok("Shop created Successfully"),
            CreateShopResult.ShopNameExist => Conflict("Shop name is already taken"),
            CreateShopResult.UserNotExist => NotFound("User not found"),
            CreateShopResult.UserHasAlreadyShop => Conflict("User has already a shop"),
            CreateShopResult.AuthenticationError => Unauthorized("Authentication error"),
            _ => StatusCode(500, "An unexpected error occurred while creating the shop")
            
        };
    }

    
}
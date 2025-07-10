using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiDbContext _dbContext;

    public ItemController(IItemService itemService, IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    {
        _itemService = itemService;
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }
    
    [HttpPost("create_item")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request)
    {
       var result = await _itemService.CreateItem(request);
       return result switch
       {
           CreateItemResult.Success => Ok("Iteam created successfully"),
           CreateItemResult.TokenInvalid => Unauthorized("Token is invalid"),
           CreateItemResult.ShopNotFound => NotFound("Shop not found"),
           CreateItemResult.UserNotFound => NotFound("User not found"),
           CreateItemResult.StockInvalid => BadRequest("Stock is invalid"),
           CreateItemResult.PriceInvalid => BadRequest("Price is invalid"),
           _ => StatusCode(500, "An unexpected error occurred while creating the item")
       };
    }
    
    [HttpPost("update-item")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateItemRequest request)
    {
        var result = await _itemService.UpdateItem(request);
        return result switch
        {
            UpdateItemResult.Success => Ok("Item updated successfully"),
            UpdateItemResult.TokenInvalid => Unauthorized("Token is invalid"),
            UpdateItemResult.ShopNotFound => NotFound("Shop not found"),
            UpdateItemResult.ItemNotFound => NotFound("Item not found"),
            UpdateItemResult.StockInvalid => BadRequest("Stock is invalid"),
            UpdateItemResult.PriceInvalid => BadRequest("Price is invalid"),
            UpdateItemResult.ConcurrencyConflict => Conflict("Item has been updated"),
            _ => StatusCode(500, "An unexpected error occurred while updating the item")
        };
    }

    [HttpPost("delete-item")]
    [Authorize(Roles = $"{nameof(UserRole.Seller)}, {nameof(UserRole.Admin)}")]
    public async Task<IActionResult> DeleteItem([FromBody] DeleteItemRequest request)
    {
        var result = await _itemService.DeleteItem(request);
        return result switch
        {
            DeleteItemResult.Success => Ok("Item deleted successfully"),
            DeleteItemResult.TokenInvalid => Unauthorized("Token is invalid"),
            DeleteItemResult.UserNotFound => NotFound("User not found"),
            DeleteItemResult.ShopNotFound => NotFound("Shop not found"),
            DeleteItemResult.ItemNotFound => NotFound("Item not found"),
            _ => StatusCode(500, "An unexpected error occurred while deleting the item")
        };
    }

    [HttpGet("search_item")]
    public async Task<IActionResult> SearchItem([FromBody] SearchItemRequest request)
    {
        var result = await _itemService.SearchItem(request);
        if (result == null)
        {
            return NotFound("Item not found");
        }
        return Ok(result);
    }
}
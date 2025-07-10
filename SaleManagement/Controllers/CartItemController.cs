using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartItemController : ControllerBase
{
    private readonly ICartItemService _cartItemService;
    

    public CartItemController(ICartItemService cartItemService)
    {
        _cartItemService = cartItemService;
       
    }

    [HttpPost("add_item_in_cart")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CartItem([FromBody] CartItemRequest request)
    {
        var result = await _cartItemService.CartItem(request);
        return result switch
        {
            CartItemResult.Success => Ok("Item added to cart successfully"),
            CartItemResult.UserNotFound => NotFound("User not found"),
            CartItemResult.TokenInvalid => Unauthorized("Token is invalid"),
            CartItemResult.ItemNotFound => NotFound("Item not found"),
            _ => StatusCode(500, "An unexpected error occurred while adding the item to the cart")
        };
    }

    [HttpPost("update_quantity_item_in_cart")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> UpdateQuantityItemInCart([FromBody] UpdateQuantityItemInCartRequest request)
    {
        var result = await _cartItemService.UpdateQuantityItemInCart(request);
        return result switch
        {
            UpdateQuantityItemInCartResult.Success => Ok("Quantity of item in cart updated successfully"),
            UpdateQuantityItemInCartResult.TokenInvalid => Unauthorized("Token is invalid"),
            UpdateQuantityItemInCartResult.UserNotFound => NotFound("User not found"),
            UpdateQuantityItemInCartResult.ItemNotFound => NotFound("Item not found"),
            UpdateQuantityItemInCartResult.QuantityInvalid => BadRequest("Quantity is invalid"),
            _ => StatusCode(500, "An unexpected error occurred while updating the quantity of the item in the cart")
        };
    }

    [HttpPost("delete_item_from_cart")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> DeleteItemFromCart([FromBody] DeleteItemFromCartRequest request)
    {
        var result = await _cartItemService.DeleteItemFromCart(request);
        return result switch
        {
            DeleteItemFromCartResult.Success => Ok("Item deleted from cart successfully"),
            DeleteItemFromCartResult.TokenInvalid => Unauthorized("Token is invalid"),
            DeleteItemFromCartResult.UserNotFound => NotFound("User not found"),
            DeleteItemFromCartResult.ItemNotFound => NotFound("Item not found"),
            _ => StatusCode(500, "An unexpected error occurred while deleting the item from the cart")
        };
    }
}
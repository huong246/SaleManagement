using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController:ControllerBase
{
    private readonly IOrderService _orderService;
   

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
        
    }

    [HttpPost("create_order")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await _orderService.CreateOrder(request);
        return result switch
        {
            CreateOrderResult.Success => Ok("Order created successfully"),
            CreateOrderResult.TokenInvalid => Unauthorized("Token is invalid"),
            CreateOrderResult.UserNotFound => NotFound("User not found"),
            CreateOrderResult.CartIsEmpty => BadRequest("Cart is empty"),
            CreateOrderResult.StockNotEnough => BadRequest("Stock is not enough"),
            CreateOrderResult.ConcurrencyConflict=> Conflict("Order has been created"),
            _ => StatusCode(500, "An unexpected error occurred while creating the order")
        };
    }

    [HttpGet("get_order_history")]
    public async Task<IActionResult> GetOrderHistoryAsync([FromBody] GetOrderHistoryAsyncRequest request)
    {
        var result = await _orderService.GetOrderHistoryAsync(request);
        if (result == null)
        {
            return NotFound("Order not found");
        }
        return Ok(result);
    }

    [HttpPost("update_order_status")]
    public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateOrderStatus(request);
        return result switch
        {
            UpdateOrderStatusResult.Success => Ok("OrderStatus updated successfully"),
            UpdateOrderStatusResult.InvalidStatusTransition => BadRequest("Invalid status transition"),
            UpdateOrderStatusResult.TokenInvalid => Unauthorized("Token is invalid"),
            UpdateOrderStatusResult.OrderNotFound => NotFound("Order not found"),
            UpdateOrderStatusResult.AuthorizeFailed => Unauthorized("Authorization failed"),
            UpdateOrderStatusResult.ConcurrencyConflict => Conflict("Order has been updated"),
            UpdateOrderStatusResult.UserNotFound => NotFound("User not found"),
            _ => StatusCode(500, "An unexpected error occurred while updating the order status")
        };
    }
    
    
}
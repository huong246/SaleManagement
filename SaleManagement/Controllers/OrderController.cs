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

    [HttpGet("get_order_history/{orderId}")]
    public async Task<IActionResult> GetOrderHistoryAsync( Guid orderId)
    {
        var result = await _orderService.GetOrderHistoryAsync(orderId);
        if (result == null|| !result.Any())
        {
            return NotFound("Order not found");
        }
        return Ok(result);
    }

    [HttpPost("update_order_status")]
    [Authorize(Roles = $"{nameof(UserRole.Seller)}, {nameof(UserRole.Admin)}")]
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

    [HttpPost("order_cancel_customer")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CancelOrder(CancelOrderRequest request)
    {
        var result = await _orderService.CancelOrder(request);
        return result switch
        {
            CancelOrderResult.Success => Ok("Order cancelled successfully"),
            CancelOrderResult.NotAllowed => BadRequest("Order is not allowed to be cancelled"),
            CancelOrderResult.TokenInvalid => Unauthorized("Token is invalid"),
            CancelOrderResult.UserNotFound => NotFound("User not found"),
            CancelOrderResult.OrderNotFound => NotFound("Order not found"),
            CancelOrderResult.AuthorizeFailed => Unauthorized("Authorization failed"),
            _ => StatusCode(500, "An unexpected error occurred while cancelling the order")
        };
    }

    [HttpPost("return_order_request")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> RequestReturn(RequestReturnRequest request)
    {
        var result = await _orderService.RequestReturn(request);
        return result switch
        {
            RequestReturnResult.Success => Ok("Request return successfully"),
            RequestReturnResult.NotAllowed => BadRequest("Order is not allowed to be returned"),
            RequestReturnResult.TokenInvalid => Unauthorized("Token is invalid"),
            RequestReturnResult.UserNotFound => NotFound("User not found"),
            RequestReturnResult.OrderNotFound => NotFound("Order not found"),
            RequestReturnResult.AuthorizeFailed => Unauthorized("Authorization failed"),
            _ => StatusCode(500, "An unexpected error occurred while requesting the return")
        };
    }

    [HttpPost("{orderId}/{complete}")]
    [Authorize]
    public async Task<IActionResult> MarkOrderAsSuccessful(ProcessPayoutForSuccessfulOrderRequest request)
    {
        var payoutResult = await _orderService.ProcessPayoutForSuccessfulOrder(request);
        if (!payoutResult)
        {
            return StatusCode(500, "da cap nhat trang thai don hang nhung thanh toan that bai");
            
        }

        return Ok("don hang da hoan tat va tien da duoc chuyen cho nguoi ban");
    }
    
}
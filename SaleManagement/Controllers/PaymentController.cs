using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }
    
    [HttpPost("payment")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> Payment(PaymentRequest request)
    {
        var result = await _paymentService.Payment(request);
        return result switch
        {
            PaymentResult.Success => Ok("Payment successful"),
            PaymentResult.TokenInvalid => Unauthorized("Token is invalid"),
            PaymentResult.UserNotFound => NotFound("User not found"),
            PaymentResult.BalanceNotEnough => BadRequest("Balance is not enough"),
            PaymentResult.OrderNotPending => BadRequest("Order is not pending"),
            PaymentResult.OrderNotFound => NotFound("Order not found"),
            PaymentResult.ConcurrencyError => Conflict("Order has been processed"),
            PaymentResult.OrderNotOwnByUser => BadRequest("Order is not owned by the user"),
            _ => StatusCode(500, "An unexpected error occurred while processing the payment")
        };
    }
}
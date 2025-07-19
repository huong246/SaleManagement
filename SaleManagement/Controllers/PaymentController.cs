using System.Text.Json;
 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
 
using SaleManagement.Data;
using SaleManagement.Entities;
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
    private readonly IMomoPaymentService _momoPaymentService;
    private readonly ApiDbContext _dbContext;
    private readonly IConfiguration _configuration;
    
    
    public PaymentController(IPaymentService paymentService, IMomoPaymentService momoPaymentService, ApiDbContext dbContext, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _momoPaymentService = momoPaymentService;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpPost("cash_in")]
    public async Task<IActionResult> CashIn(CashInRequest request)
    {
        var result = await _paymentService.CashIn(request);
        return result switch
        {
            CashInResult.Success => Ok("Cash in successfully"),
            CashInResult.CashInAmountInvalid => BadRequest("Cash in amount is invalid"),
            CashInResult.TokenInvalid => BadRequest("Token in valid"),
            CashInResult.UserNotFound => NotFound("User not found"),
            _ => StatusCode(500, "An unexpected error occurred while processing the cash in")
        };
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

    [HttpPost("momo_payment")]
    public async Task<IActionResult> CreateMomoPayment(Guid orderId)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order == null) return NotFound("Order not found.");

        var response = await _momoPaymentService.CreateMomoPaymentAsync(order);
        if (response.resultCode == null)
        {
            return Ok(response);  
        }
        return BadRequest(response.message);
    }
    
    
 [HttpPost("momo-notify")]
    public async Task<IActionResult> MomoNotify([FromBody] JsonElement body)
    {
       
        var result = await _momoPaymentService.ProcessIpnResponseAsync(body);

        if (result == IpnProcessResult.Success)
        {
             
            return NoContent(); 
        }

        return BadRequest("IPN processing failed.");
    }
}
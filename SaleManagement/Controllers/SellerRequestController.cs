using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities.Enums;
using SaleManagement.Services;

namespace SaleManagement.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SellerRequestController :ControllerBase
{
    private readonly ISellerRequestService _sellerRequestService;

    public SellerRequestController(ISellerRequestService sellerRequestService)
    {
        _sellerRequestService = sellerRequestService;
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CreateRequest()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var result = await _sellerRequestService.CreateRequestAsync(userId);
        if(!result)
        {
            return BadRequest("Request already exists");
        }
        return Ok("Request created successfully");
    }
    
    [HttpGet("pending")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> GetPendingRequests()
    {
        var result = await _sellerRequestService.GetPendingRequestsAsync();
        return Ok(result);
    }
    
    [HttpPost("{requestId}/approve")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> ApproveRequest(Guid requestId)
    {
        var result = await _sellerRequestService.ApproveRequestAsync(requestId);
        return result ? Ok("Request approved successfully") : BadRequest("Request not found");
    }
    
    [HttpPost("{requestId}/reject")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> RejectRequest(Guid requestId)
    {
        var result = await _sellerRequestService.RejectRequestAsync(requestId);
        return result ? Ok("Request rejected successfully") : BadRequest("Request not found");
    }
}
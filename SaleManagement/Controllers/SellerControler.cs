using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities.Enums;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Seller))]
public class SellerControler :ControllerBase
{
    private readonly ISellerService _sellerService;
    public SellerControler(ISellerService sellerService)
    {
        _sellerService = sellerService;
    }
    
    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashbroadStats()
    {
        var stats = await _sellerService.GetSellerDashboardStatsAsync();
        return Ok(stats);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {

        var orders = await _sellerService.GetShopOrderAsync();
        return Ok(orders);
    }
}
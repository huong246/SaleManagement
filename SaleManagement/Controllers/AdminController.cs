using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities.Enums;
using SaleManagement.Services;

namespace SaleManagement.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashbroad/stats")]
    public async Task<IActionResult> GetDashbroadStats()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        return Ok(stats);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _adminService. GetAllUserAsync();
        return Ok(users);
    }

    [HttpGet("shops")]
    public async Task<IActionResult> GetAllShops()
    {
        var shops = await _adminService.GetAllShopAsync();
        return Ok(shops);
    }
}
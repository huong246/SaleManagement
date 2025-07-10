using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Data;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VoucherController : ControllerBase
{
    private readonly IVoucherService _voucherService;
    
    public VoucherController(IVoucherService voucherService)
    
    {
      _voucherService = voucherService;
        
    }

    [HttpPost("create_voucher")]
    [Authorize(Roles = $"{nameof(UserRole.Seller)}, {nameof(UserRole.Admin)}")]
    public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherRequest request)
    {
        var result = await _voucherService.CreateVoucher(request);
        return result switch
        {
            CreateVoucherResult.Success => Ok("Voucher created successfully"),
            CreateVoucherResult.TokenInvalid => Unauthorized("Token is invalid"),
            CreateVoucherResult.UserNotFound => NotFound("User not found"),
            CreateVoucherResult.ShopNotFound => NotFound("Shop not found"),
            CreateVoucherResult.ItemNotFound => NotFound("Item not found"),
            CreateVoucherResult.QuantityInvalid => BadRequest("Quantity is invalid"),
            _ => StatusCode(500, "An unexpected error occurred while creating the voucher")

        };
    }
    
    [HttpPost("delete_voucher")]
    [Authorize(Roles = $"{nameof(UserRole.Seller)}, {nameof(UserRole.Admin)}")]
    public async Task<IActionResult> DeleteVoucher([FromBody] DeleteVoucherRequest request)
    {
        var result = await _voucherService.DeleteVoucher(request);
        return result switch
        {
            DeleteVoucherResult.Success => Ok("Voucher deleted successfully"),
            DeleteVoucherResult.TokenInvalid => Unauthorized("Token is invalid"),
            DeleteVoucherResult.UserNotFound => NotFound("User not found"),
            DeleteVoucherResult.VoucherNotFound => NotFound("Voucher not found"),
            DeleteVoucherResult.ShopNotFound => NotFound("Shop not found"),
            _ => StatusCode(500, "An unexpected error occurred while deleting the voucher")
        };
    }

    [HttpPost("update_voucher")]
    [Authorize(Roles = $"{nameof(UserRole.Seller)}, {nameof(UserRole.Admin)}")]
    public async Task<IActionResult> UpdateVoucher([FromBody] UpdateVoucherRequest request)
    {
        var result = await _voucherService.UpdateVoucher(request);
        return result switch
        {
            UpdateVoucherResult.Success => Ok("Voucher updated successfully"),
            UpdateVoucherResult.TokenInvalid => Unauthorized("Token is invalid"),
            UpdateVoucherResult.UserNotFound => NotFound("User not found"),
            UpdateVoucherResult.VoucherNotFound => NotFound("Voucher not found"),
            UpdateVoucherResult.ConcurrencyConflict => Conflict("Voucher has been updated"),
            _ => StatusCode(500, "An unexpected error occurred while updating the voucher")
        };
    }
    
}
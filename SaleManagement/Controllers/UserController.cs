using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/users/me")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("get_user_profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var user = await _userService.GetUserProfileAsync();
        return Ok(user);
    }
    

    [HttpPost("update_user_profile")]
    public async Task<IActionResult> UpdateUserProfileAsync(UpdateProfileRequest request)
    {
        var result = await _userService.UpdateUserProfileAsync(request);
        return result switch
        {
            UpdateUserProfileResult.Success => Ok("UserProfile Updated Successfully"),
            UpdateUserProfileResult.TokenInvalid => BadRequest("Invalid Token"),
            UpdateUserProfileResult.UserNotFound => NotFound("User not found"),
            _ => StatusCode(500, "An unexpected error occurred while updating"),
        };
    }

    [HttpPost("create_address_user")]
    public async Task<IActionResult> CreateAddressUserAsync(CreateAddressRequest request)
    {
        var result = await _userService.CreateAddressAsync(request);
        return Ok(result);
    }
    
    [HttpGet("get_user_address")]
    public async Task<IActionResult> GetUserAddress()
    {
        var address = await _userService.GetAddressesAsync();
        return Ok(address);
    }

    [HttpPost("update_user_address")]
    public async Task<IActionResult> UpdateAddressUserAsync(UpdateAddressRequest request)
    {
        var result = await _userService.UpdateAddressAsync(request);
        return result switch
        {
            UpdateAddressResult.Success => Ok("Address Updated Successfully"),
            UpdateAddressResult.TokenInvalid => BadRequest("Invalid Token"),
            UpdateAddressResult.UserNotFound => NotFound("User not found"),
            UpdateAddressResult.AdressNotFound => NotFound("Adress not found"),
            _ => StatusCode(500, "An unexpected error occurred while updating address")
        };
    }

    [HttpDelete("delete_user_address")]
    public async Task<IActionResult> DeleteAddressUserAsync(DeleteAddressRequest request)
    {
        var result = await _userService.DeleteAddressAsync(request);
        return result switch
        {
            DeleteAddressResult.Success => Ok("Address Deleted Successfully"),
            DeleteAddressResult.TokenInvalid => BadRequest("Invalid Token"),
            DeleteAddressResult.UserNotFound => NotFound("User not found"),
            DeleteAddressResult.AddressNotFound => NotFound("Address not found"),
            _ => StatusCode(500, "An unexpected error occurred while deleting")
        };
    }
    
}
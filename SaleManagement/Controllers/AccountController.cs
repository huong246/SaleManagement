using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]

public class AccountController: ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ApiDbContext _dbContext;
    public AccountController(IAccountService accountService, ApiDbContext dbContext)
    {
        _accountService = accountService;
        _dbContext = dbContext;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
    {
        var result = await _accountService.CreateUser(request);
        return result switch
        {
            CreateUserResult.Success => Ok("User created successfully."),
            CreateUserResult.UsernameExist=> Conflict($"Username '{request.Username}' is already taken."),
            _ => StatusCode(500, "An unexpected error occurred while creating the user.")
        };
    }

    [HttpPost("login")]
    
    public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
    {
       var result = await _accountService.LoginUser(request);
       if (result.LonginUserResultType == LoginUserResultType.Success)
       {
           return Ok(new{token = result.AccessToken});
       }
       return Unauthorized("username or password is incorrect");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return BadRequest("Invalid user id");
        }

        await _accountService.LogoutAsync();
        return Ok("Logged out successfully");
    }
    
    [HttpGet("transaction")]
    [Authorize]
    public async Task<IActionResult> GetTransaction()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    
        var transactions = await _dbContext.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();

        return Ok(transactions);
    }
    
}
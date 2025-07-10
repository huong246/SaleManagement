using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class AccountService : IAccountService
{
    private readonly ApiDbContext _dbContext;
    private readonly IConfiguration _configuration; 
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    public AccountService(ApiDbContext dbContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _configuration = configuration; 
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }
    
    public async Task<CreateUserResult> CreateUser(CreateUserRequest request)
    {
        var username = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (username != null)
        {
            return CreateUserResult.UsernameExist;
        }

        var newUser = new User()
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Balance = 0,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            UserRoles = UserRole.Customer,
        };
        try
        {

            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();
            return CreateUserResult.Success;
        }
        catch (DbUpdateException)
        {
            return CreateUserResult.DatabaseError;
        }
        
    }

    public async Task<LoginUserResult> LoginUser(LoginUserRequest request )
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return new LoginUserResult(LoginUserResultType.InvalidCredentials, null, null);
        }

        var authClaim = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
        foreach (UserRole role in Enum.GetValues(typeof(UserRole)))
        {
            // phương thức HasFlag()
            if (user.UserRoles.HasFlag(role) && role != UserRole.None)
            {
                authClaim.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }
        }

        var accessToken = GenerateAccessToken(authClaim);
        var refreshToken = GenerateRefreshToken();
        
        _= int.TryParse(_configuration["Jwt:RefreshTokenExpiresInDays"], out var refreshTokenExpiresInDays);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiresInDays);
        
        await _dbContext.SaveChangesAsync();
        return new LoginUserResult(LoginUserResultType.Success, accessToken, refreshToken);
    }
    
    private string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var tokenExpires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiresInMinutes"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: tokenExpires,
            claims: claims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<LogoutUserResultType> LogoutAsync()
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return LogoutUserResultType.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return LogoutUserResultType.UserNotFound;
        }

        var jti = _httpContextAccessor.HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var exp = _httpContextAccessor.HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (jti != null && exp != null)
        {
            var expiryTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(exp));
            var cacheExpiry = expiryTime - DateTimeOffset.UtcNow;
            if (cacheExpiry > TimeSpan.Zero)
            {
                _cache.Set(jti, "blacklisted", cacheExpiry);
            }
        }
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            try
            {
                await _dbContext.SaveChangesAsync();
                return LogoutUserResultType.Success;
            }
            catch (DbUpdateException)
            {
                return LogoutUserResultType.DatabaseError;
            }
       
    }
    
    
}
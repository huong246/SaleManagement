using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class UserService : IUserService
{
    private readonly ApiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(ApiDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }

        return await _dbContext.Users.Where(u => u.Id == userId)
            .Select(u => new UserProfileDto(u.Id, u.Username, u.Fullname, u.PhoneNumber, u.Birthday, u.Gender))
            .FirstOrDefaultAsync();
    }

    public async Task<UpdateUserProfileResult> UpdateUserProfileAsync(UpdateProfileRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UpdateUserProfileResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return UpdateUserProfileResult.UserNotFound;
        }

        user.Fullname = request.FullName ?? user.Fullname;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.Gender = request.Gender?? user.Gender;
        user.Birthday = request.Birthday?? user.Birthday;
        try
        {
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            return UpdateUserProfileResult.Success;
        }
        catch (DbUpdateException)
        {
            return UpdateUserProfileResult.DatabaseError;
        }

    }

    public async Task<IEnumerable<AddressDto>> GetAddressesAsync()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Enumerable.Empty<AddressDto>();
        }
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return Enumerable.Empty<AddressDto>();
        }

        return await _dbContext.UserAddresses.Where(a => a.User == user)
            .Select(a => new AddressDto(a.Id, a.Name, a.Latitude, a.Longitude, a.IsDefault)).ToListAsync();
        
    }

    public async Task<AddressDto?> CreateAddressAsync(CreateAddressRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return null;
        }

        if (request.IsDefault)
        {
            var currentDefault = await _dbContext.UserAddresses.FirstOrDefaultAsync(a => a.User == user && a.IsDefault);
            if(currentDefault != null) currentDefault.IsDefault = false;
        }
        var newAddress = new UserAddress()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            User = user,
            UserId = userId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsDefault = request.IsDefault
        };
        _dbContext.UserAddresses.Add(newAddress);
        await _dbContext.SaveChangesAsync();
        return new AddressDto(newAddress.Id, newAddress.Name, newAddress.Latitude, newAddress.Longitude, newAddress.IsDefault);
    }

    public async Task<UpdateAddressResult> UpdateAddressAsync(UpdateAddressRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UpdateAddressResult.TokenInvalid;
        }
        var user = await _dbContext.Users.SingleAsync(u => u.Id == userId);
        if (user == null)
        {
            return UpdateAddressResult.UserNotFound;
        }

        var address = await _dbContext.UserAddresses.FirstOrDefaultAsync(a => a.User == user && a.Id == request.AddressId);
        if (address == null)
        {
            return UpdateAddressResult.AdressNotFound;
        }

        if (request.IsDefault)
        {
            var currentDefault = await _dbContext.UserAddresses.FirstOrDefaultAsync(a => a.User == user && a.IsDefault);
            if(currentDefault != null) currentDefault.IsDefault = false;
        }
        address.Latitude = request.Latitude;
        address.Longitude = request.Longitude;
        address.IsDefault = request.IsDefault;
        address.Name = request.Name;
        try
        {
            _dbContext.UserAddresses.Update(address); 
            await _dbContext.SaveChangesAsync();
            return UpdateAddressResult.Success;
        }
        catch (DbUpdateException)
        {
            return UpdateAddressResult.DatabaseError;
        }
       
    }

    public async Task<DeleteAddressResult> DeleteAddressAsync(DeleteAddressRequest request)
    {
        
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return DeleteAddressResult.TokenInvalid;
            }
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return DeleteAddressResult.UserNotFound;
            }
            var address = await _dbContext.UserAddresses.FirstOrDefaultAsync(a => a.User == user && a.Id == request.AddressId);
            if (address == null)
            {
                return DeleteAddressResult.AddressNotFound;
            }

            try
            {
             _dbContext.UserAddresses.Remove(address);
             await _dbContext.SaveChangesAsync();
             return DeleteAddressResult.Success;
            }
            catch (DbUpdateException)
            {
               return DeleteAddressResult.DatabaseError;
            }
    }
    
    
}
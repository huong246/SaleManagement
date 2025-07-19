using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Hubs;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class ShopService : IShopService
{
    private readonly ApiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    
    public ShopService(ApiDbContext dbContext, IHttpContextAccessor httpContextAccessor, IHubContext<NotificationHub> notificationHubContext)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _notificationHubContext = notificationHubContext;
    }

    public async Task<CreateShopResult> CreateShop(CreateShopRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return CreateShopResult.AuthenticationError;
        }
        var User = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (User == null)
        {
            return CreateShopResult.UserNotExist;
        }
        var userHasShop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == User.Id);
        if (userHasShop != null)
        {
            return CreateShopResult.UserHasAlreadyShop;
        }
        var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.Name == request.Name);
        if (shop != null)
        {
            return CreateShopResult.ShopNameExist;
        }
     
        var newShop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            UserId = User.Id,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PreparationTime = request.PreparationTime
        };
        
        try
        {
            _dbContext.Shops.Add(newShop);
            await _dbContext.SaveChangesAsync();
            var userMessage = $"Tao shop thanh cong";
            await _notificationHubContext.Clients.User(User.Id.ToString()).SendAsync("ReceiveMessage", userMessage);
            return CreateShopResult.Success;
        }
        catch (DbUpdateException)
        {
            return CreateShopResult.DatabaseError;
        }
    }
}
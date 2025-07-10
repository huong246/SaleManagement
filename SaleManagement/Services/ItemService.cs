using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class ItemService : IItemService
{
    private readonly ApiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public ItemService(ApiDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CreateItemResult> CreateItem(CreateItemRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return CreateItemResult.TokenInvalid;
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return CreateItemResult.UserNotFound;
        }
        var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (shop == null)
        {
            return CreateItemResult.ShopNotFound;
        }

        if (request.stock <= 0)
        {
            return CreateItemResult.StockInvalid;
        }

        if (request.Price < 0)
        {
            return CreateItemResult.PriceInvalid;
        }

        var newItem = new Item()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Price = request.Price,
            stock = request.stock,
            ShopId = shop.Id,
            Description = request.Description,
            CategoryId = request.CategoryId,
        };
        try
        {
            _dbContext.Items.Add(newItem);
            await _dbContext.SaveChangesAsync();
            return CreateItemResult.Success;
        }
        catch (DbUpdateException)
        {
            return CreateItemResult.DatabaseError;
        }
    }

    public async Task<UpdateItemResult> UpdateItem(UpdateItemRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return UpdateItemResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return UpdateItemResult.UserNotFound;
        }
        var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (shop == null)
        {
            return UpdateItemResult.ShopNotFound;
        }

        var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null)
        {
            return UpdateItemResult.ItemNotFound;
        }

        if (request.Stock != null && request.Stock < 0)
        {
            return UpdateItemResult.StockInvalid;
        }

        if (request.Price != null && request.Price < 0)
        {
            return UpdateItemResult.PriceInvalid;
        }

        _dbContext.Entry(item).Property("RowVersion").OriginalValue = request.RowVersion;
        
        item.Name = request.Name;
        item.Price = (decimal)request.Price!;
        item.stock = (int)request.Stock!;
        item.Description = request.Description;
        item.CategoryId = request.CategoryId;
        try
        {
            _dbContext.Items.Update(item);
            await _dbContext.SaveChangesAsync();
            return UpdateItemResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return UpdateItemResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            return UpdateItemResult.DatabaseError;
        }
        
    }

    public async Task<DeleteItemResult> DeleteItem(DeleteItemRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return DeleteItemResult.TokenInvalid;
        }
        
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return DeleteItemResult.UserNotFound;
        }
        var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (shop == null)
        {
            return DeleteItemResult.ShopNotFound;
        }
     
        var item = await _dbContext.Items.FirstOrDefaultAsync(i=>i.Id == request.ItemId);
        if (item == null)
        {
            return DeleteItemResult.ItemNotFound;
        }

        if (!user.UserRoles.HasFlag(UserRole.Admin) || !user.UserRoles.HasFlag(UserRole.Seller))
        {
            return DeleteItemResult.ShopNotFound;
        }

        else if (item.ShopId != shop.Id)
        {
            return DeleteItemResult.ShopNotFound;
        }
      
        try
        {
            _dbContext.Items.Remove(item);
            await _dbContext.SaveChangesAsync();
            return DeleteItemResult.Success;
        }
       catch (DbUpdateException)
        {
            return DeleteItemResult.DatabaseError;
        }
    }

    public async Task<IEnumerable<Item>> SearchItem(SearchItemRequest request)
    {
        var query = _dbContext.Items.Include(i => i.Category).AsQueryable();
        if (!string.IsNullOrEmpty(request.Keyword)) ;
        {
            query = query.Where(i => i.Name.Contains(request.Keyword)|| i.Description.Contains(request.Keyword));
        }

        if (request.CategoryId.HasValue &&request.CategoryId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CategoryId == request.CategoryId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(i => i.Price >= request.MinPrice.Value);
        }
        if (request.MaxPrice.HasValue)
        {
            query = query.Where(i => i.Price <= request.MaxPrice.Value);
        }
        
        return await query.ToListAsync();
    }
}
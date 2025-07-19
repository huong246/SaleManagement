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

        if (request.Stock <= 0)
        {
            return CreateItemResult.StockInvalid;
        }

        if (request.Price < 0)
        {
            return CreateItemResult.PriceInvalid;
        }

        if (request.CategoryId.HasValue)
        {
            var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId);
            if (category == null)
            {
                return CreateItemResult.CategoryNotFound;
            }
        }
        var newItem = new Item()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Price = request.Price,
            Stock = request.Stock,
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
        catch (DbUpdateException ex)
        {
            // Ghi lại lỗi để kiểm tra (sử dụng logger trong dự án thực tế)
            Console.WriteLine(ex.ToString()); // Dùng để debug
            if (ex.InnerException != null)
            {
                Console.WriteLine("Inner Exception: " + ex.InnerException.Message); // Dòng này rất quan trọng!
            }
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

        if (request.Stock is null or < 0)
        {
            return UpdateItemResult.StockInvalid;
        }

        if (request.Price is null or < 0)
        {
            return UpdateItemResult.PriceInvalid;
        }
        

        _dbContext.Entry(item).Property("RowVersion").OriginalValue = request.RowVersion;
        
        item.Name = request.Name ?? item.Name;
        item.Price = request.Price ?? item.Price;
        item.Stock = request.Stock ?? item.Stock;
        item.Description = request.Description ?? item.Description;
        item.CategoryId = request.CategoryId ?? item.CategoryId;
      
        try
        {
            
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
       
     
        var item = await _dbContext.Items.FirstOrDefaultAsync(i=>i.Id == request.ItemId );
        if (item == null)
        {
            return DeleteItemResult.ItemNotFound;
        }

        try
        {
        var isAdmin = user.UserRoles.HasFlag(UserRole.Admin);
        var isSellerAndIsUser = user.UserRoles.HasFlag(UserRole.Seller);
        if (isAdmin)
        {
            _dbContext.Items.Remove(item);
            await _dbContext.SaveChangesAsync();
            return DeleteItemResult.Success;
        }

        if (isSellerAndIsUser)
        {
            var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (shop == null)
            {
                return DeleteItemResult.ShopNotOwner;
            }

            if (shop != null && item.ShopId == shop.Id)
            {
                _dbContext.Items.Remove(item);
                await _dbContext.SaveChangesAsync();
                return DeleteItemResult.Success;
            }
        }

        return DeleteItemResult.UserNotPermission;
        }
        catch (DbUpdateException)
        {
            return DeleteItemResult.DatabaseError;
        }
       
    }
    

    public async Task<IEnumerable<Item>> SearchItem(SearchItemRequest request)
    {
        var query = _dbContext.Items.Include(i => i.Category).AsQueryable();
        if (!string.IsNullOrEmpty(request.Keyword)) 
        {
            var searchTerm = $"\"{request.Keyword.Replace("\"", "\"\"")}\"*"; // Thêm * để tìm kiếm theo tiền tố
             
            var matchingItemIds = await _dbContext.Items
                .FromSqlRaw("SELECT * FROM Items WHERE Id IN (SELECT rowid FROM ItemsFTS WHERE ItemsFTS MATCH {0})", searchTerm)
                .Select(i => i.Id)
                .ToListAsync();
            
            query = query.Where(i => matchingItemIds.Contains(i.Id));
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

        if (!string.IsNullOrEmpty(request.Size))
        {
            query = query.Where(i => i.Size != null && i.Size.ToLower() == request.Size.ToLower());
        }

        switch (request.SortBy?.ToLower())
        {
            case"price_asc":
                query = query.OrderBy(i => i.Price);
                break;
            case"price_desc":
                query = query.OrderByDescending(i => i.Price);
                break;
            case"newest":
                query = query.OrderByDescending(i => i.Id); 
                break;
            case "best_selling":
                query = query.OrderByDescending(i => i.SaleCount);
                break;
            default:
                query = query.OrderByDescending(i => i.Name);
                break;
          
        }
        return await  query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync();
    }
}
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class CartItemService : ICartItemService
{
    private readonly ApiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartItemService(ApiDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<CartItemResult> CartItem(CartItemRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return CartItemResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Username == username);
        if (user == null)
        {
            return CartItemResult.UserNotFound;
        }
        var item = await _dbContext.Items.FirstOrDefaultAsync(i=>i.Id == request.ItemId);
        if (item == null)
        {
            return CartItemResult.ItemNotFound;
        }

        if (request.Quantity < 0)
        {
            return CartItemResult.QuantityInvalid;
        }

        if (item.stock <= 0)
        {
            return CartItemResult.OutOfStock;
        }
        var itemInCart = await _dbContext.CartItems.FirstOrDefaultAsync(ci => ci.ItemId == request.ItemId && ci.UserId == user.Id);
        if (itemInCart != null)
        {
            itemInCart.Quantity += request.Quantity;
            if (itemInCart.Quantity > item.stock)
            {
                return CartItemResult.InsufficientStock;
            }
            _dbContext.CartItems.Update(itemInCart);
        }
        else
        {
            if (item.stock < request.Quantity)
            {
                return CartItemResult.InsufficientStock;
            }

            var newCartItem = new CartItem()
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                UserId = user.Id,
                Quantity = request.Quantity,
            };
            _dbContext.CartItems.Add(newCartItem);
        }
        try
        {
            await _dbContext.SaveChangesAsync();
            return CartItemResult.Success;
        }
        catch (DbUpdateException)
        {
            return CartItemResult.DatabaseError;
        }
        
    }

    public async Task<UpdateQuantityItemInCartResult> UpdateQuantityItemInCart(UpdateQuantityItemInCartRequest request)
    {
         var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
         if (username == null)
         {
             return UpdateQuantityItemInCartResult.TokenInvalid;
         }
         var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Username == username);
         if (user == null)
         {
             return UpdateQuantityItemInCartResult.UserNotFound;
         }
         var itemInCart = await _dbContext.CartItems.FirstOrDefaultAsync(ci => ci.ItemId == request.ItemId && ci.UserId == user.Id);
         if (itemInCart == null)
         {
             return UpdateQuantityItemInCartResult.ItemNotFound;
         }

         if (request.Quantity < 0)
         {
             return UpdateQuantityItemInCartResult.QuantityInvalid;
         }
         itemInCart.Quantity = request.Quantity;
         try
         {
             _dbContext.CartItems.Update(itemInCart);
             await _dbContext.SaveChangesAsync();
             return UpdateQuantityItemInCartResult.Success;
         }
         catch (DbUpdateException)
         {
             return UpdateQuantityItemInCartResult.DatabaseError;
         }
    }

    public async Task<DeleteItemFromCartResult> DeleteItemFromCart(DeleteItemFromCartRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return DeleteItemFromCartResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Username == username);
        if (user == null)
        {
            return DeleteItemFromCartResult.UserNotFound;
        }
        var itemInCart = await _dbContext.CartItems.FirstOrDefaultAsync(ci => ci.ItemId == request.ItemId && ci.UserId == user.Id);
        if (itemInCart == null)
        {
            return DeleteItemFromCartResult.ItemNotFound;
        }
        _dbContext.CartItems.Remove(itemInCart);
        try
        {
            await _dbContext.SaveChangesAsync();
            return DeleteItemFromCartResult.Success;
        }
        catch (DbUpdateException)
        {
            return DeleteItemFromCartResult.DatabaseError;
        }
    }
}
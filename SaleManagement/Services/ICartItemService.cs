using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;


public enum CartItemResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    QuantityInvalid,
    OutOfStock,
    InsufficientStock,
    ItemNotFound,
}

public enum UpdateQuantityItemInCartResult
{
    Success,
    DatabaseError,
    QuantityInvalid,
    UserNotFound,
    TokenInvalid,
    ItemNotFound,
}

public enum DeleteItemFromCartResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    ItemNotFound,
}
public interface ICartItemService
{
    Task<CartItemResult> CartItem(CartItemRequest request);
    Task<UpdateQuantityItemInCartResult> UpdateQuantityItemInCart(UpdateQuantityItemInCartRequest request);
    Task<DeleteItemFromCartResult> DeleteItemFromCart(DeleteItemFromCartRequest request);
    
}
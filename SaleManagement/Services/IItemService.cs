using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum CreateItemResult
{
    Success,
    DatabaseError,
    ShopNotFound,
    UserNotFound,
    TokenInvalid,
    StockInvalid,
    PriceInvalid,
    CategoryNotFound,
}

public enum UpdateItemResult
{
    Success,
    DatabaseError,
    ItemNotFound,
    TokenInvalid,
    StockInvalid,
    ShopNotFound,
    UserNotFound,
    PriceInvalid,

    ConcurrencyConflict,
}

public enum DeleteItemResult
{
    Success,
    DatabaseError,
    ItemNotFound,
    TokenInvalid,
    UserNotFound,
    ShopNotOwner,
    UserNotPermission,
}

public interface IItemService
{
    Task<CreateItemResult> CreateItem(CreateItemRequest request);
    Task<UpdateItemResult> UpdateItem(UpdateItemRequest request);
    Task<DeleteItemResult> DeleteItem(DeleteItemRequest request);
    Task<IEnumerable<Item>> SearchItem(SearchItemRequest request);
}
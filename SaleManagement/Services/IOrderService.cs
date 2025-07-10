using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum CreateOrderResult
{
    Success,
    VoucherExpired,
    CartIsEmpty,
    UserNotFound,
    TokenInvalid,
    DatabaseError,
    StockNotEnough,
    MinspendNotMet,
    ShopNotFound,
    ConcurrencyConflict,
}

public enum UpdateOrderStatusResult
{
    Success,
    DatabaseError,
    OrderNotFound,
    ConcurrencyConflict,
    TokenInvalid,
    UserNotFound,
    AuthorizeFailed,
    InvalidStatusTransition,
    
}

public interface IOrderService
{
    Task<CreateOrderResult> CreateOrder(CreateOrderRequest request);
    Task<UpdateOrderStatusResult> UpdateOrderStatus(UpdateOrderStatusRequest request);
    Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(GetOrderHistoryAsyncRequest request);
}
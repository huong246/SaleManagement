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

public enum CancelOrderResult
{
    Success,
    OrderNotFound,
    NotAllowed,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    AuthorizeFailed,
}

public enum RequestReturnResult
{
    Success,
    OrderNotFound,
    NotAllowed,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    AuthorizeFailed,
}

 

public interface IOrderService
{
    Task<CreateOrderResult> CreateOrder(CreateOrderRequest request);
    Task<UpdateOrderStatusResult> UpdateOrderStatus(UpdateOrderStatusRequest request);
    Task<CancelOrderResult> CancelOrder(CancelOrderRequest request);
    Task<RequestReturnResult> RequestReturn(RequestReturnRequest request);
    Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(Guid orderId);
    Task<bool> ProcessPayoutForSuccessfulOrder(ProcessPayoutForSuccessfulOrderRequest request);
}
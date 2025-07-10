using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum PaymentResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    OrderNotFound,
    BalanceNotEnough,
    OrderNotOwnByUser,
    OrderNotPending,
    ConcurrencyError,
}
public interface IPaymentService
{
    Task<PaymentResult> Payment(PaymentRequest request);
}
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum CashInResult
{
    Success, 
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    CashInAmountInvalid,
}
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
    Task<CashInResult> CashIn(CashInRequest request);
    Task<PaymentResult> Payment(PaymentRequest request);
   
}
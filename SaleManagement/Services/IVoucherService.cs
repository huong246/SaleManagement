using SaleManagement.Schemas;

namespace SaleManagement.Services;


public enum CreateVoucherResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    ShopNotFound,
    ItemNotFound,
    QuantityInvalid,
}

public enum DeleteVoucherResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    VoucherNotFound,
    ShopNotFound,
}

public enum UpdateVoucherResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    VoucherNotFound,
    ConcurrencyConflict,
}
public interface IVoucherService
{
 Task<CreateVoucherResult> CreateVoucher(CreateVoucherRequest request);   
 Task<DeleteVoucherResult> DeleteVoucher(DeleteVoucherRequest request);  
 Task<UpdateVoucherResult> UpdateVoucher(UpdateVoucherRequest request);
}
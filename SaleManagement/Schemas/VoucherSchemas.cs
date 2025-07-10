using SaleManagement.Entities.Enums;

namespace SaleManagement.Schemas;

public record CreateVoucherRequest(Guid? ShopId, Guid? ItemId, int LengthCode, int Quantity, decimal DiscountValue, DiscountMethod MethodType, VoucherTarger TargetType, decimal? Minspend, decimal? MaxDiscountAmount, DateTime ValidFrom, DateTime ValidUntil, bool IsActive);
public record DeleteVoucherRequest(Guid VoucherId);
public record UpdateVoucherRequest(Guid VoucherId, Guid? ShopId, Guid? ItemId, int LengthCode, int Quantity, decimal DiscountValue, DiscountMethod MethodType, VoucherTarger TargetType, decimal? Minspend, decimal? MaxDiscountAmount, DateTime ValidFrom, DateTime ValidUntil, bool IsActive, byte[] RowVersion);
using SaleManagement.Entities.Enums;

namespace SaleManagement.Schemas;

public record CreateOrderRequest(Guid? VoucherProductId, Guid? VoucherShippingId, double? ShippingLatitude, double? ShippingLongtitude);
public record UpdateOrderStatusRequest(Guid OrderId, OrderStatus Status, string? Note);

public record GetOrderHistoryAsyncRequest(Guid OrderId);
using SaleManagement.Entities.Enums;

namespace SaleManagement.Schemas;

public record CreateOrderRequest(List<Guid> ItemIds,Guid? VoucherProductId, Guid? VoucherShippingId, double? ShippingLatitude, double? ShippingLongtitude);
public record UpdateOrderStatusRequest(Guid OrderId, OrderStatus Status, string? Note);

public record CancelOrderRequest(Guid OrderId);
public record RequestReturnRequest(Guid OrderId, string Reason);

public record GetOrderHistoryAsyncRequest(Guid OrderId);

public record ProcessPayoutForSuccessfulOrderRequest(Guid OrderId);
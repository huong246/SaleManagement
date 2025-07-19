namespace SaleManagement.Schemas;

public record MomoPaymentRequest(string PartnerCode, string RequestId, long Amount, string OrderId, string OrderInfo, string RedirectUrl, string IpnUrl, string RequestType, string ExtraData, string Lang, string Signature);
public record MomoPaymentResponse(string partnerCode, string requestId, string orderId, long amount, long responseTime, string message, string resultCode, string payUrl, string deeplink);
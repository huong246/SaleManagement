namespace SaleManagement.Schemas;

public record CashInRequest(decimal Amount);
public record PaymentRequest(Guid OrderId);


namespace SaleManagement.Schemas;

public record CartItemRequest(Guid ItemId, int Quantity);

public record UpdateQuantityItemInCartRequest(Guid ItemId, int Quantity);

public  record DeleteItemFromCartRequest(Guid ItemId);
namespace SaleManagement.Schemas;

public record CreateItemRequest(string Name, string Description, decimal Price, int stock, Guid? CategoryId);

public record UpdateItemRequest(Guid ItemId, string? Name, string? Description, decimal? Price, int? Stock, Guid? CategoryId, byte[] RowVersion);

public record DeleteItemRequest(Guid ItemId);
public record SearchItemRequest(string? Keyword, Guid? CategoryId, decimal? MinPrice, decimal? MaxPrice);
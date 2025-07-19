namespace SaleManagement.Schemas;

public record CreateItemRequest(string Name, string Description, decimal Price, int Stock, Guid? CategoryId);

public record UpdateItemRequest(Guid ItemId, string? Name, string? Description, decimal? Price, int? Stock, Guid? CategoryId, byte[] RowVersion);

public record DeleteItemRequest(Guid ItemId);
public record SearchItemRequest(string? Keyword, Guid? CategoryId, decimal? MinPrice, decimal? MaxPrice, string? Color, string? Size, string? SortBy, int PageNumber =1, int PageSize =10);
//sortby la sap xep theo tieu chi cu the
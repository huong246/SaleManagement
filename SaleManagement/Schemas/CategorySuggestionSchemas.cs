namespace SaleManagement.Schemas;

 
    public record CreateCategorySuggestionRequest(string Name, string? Description);

    public record CategorySuggestionDto(Guid Id, string Name, string? Description, Guid? RequesterId, string RequesterName, string Status);
    
    
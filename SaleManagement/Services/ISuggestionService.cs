using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum SuggestionResult
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
    NotFound,
    NotPending,
    AlreadyExists,
   
}

public interface ISuggestionService
{
    Task<SuggestionResult> ApproveSuggestionAsync(Guid suggestionId);
    Task<SuggestionResult> RejectSuggestionAsync(Guid suggestionId);
    Task<SuggestionResult> CreateSuggestionAsync(CreateCategorySuggestionRequest request);
    Task<List<CategorySuggestionDto>> GetPendingSuggestionsAsync();
}
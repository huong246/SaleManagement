using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class SuggestionService: ISuggestionService
{
    private readonly ApiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SuggestionService(ApiDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<SuggestionResult> CreateSuggestionAsync(CreateCategorySuggestionRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return SuggestionResult.TokenInvalid;
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return SuggestionResult.UserNotFound;
        }
        
        var newSuggestion = new CategorySuggestion
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            RequesterId = user.Id,
            Status = RequestStatus.Pending,
        };
        try
        {
            await _dbContext.CategorySuggestions.AddAsync(newSuggestion);
            await _dbContext.SaveChangesAsync();
            return SuggestionResult.Success;
        }
        catch
        {
            return SuggestionResult.DatabaseError;
        }
    }

    public async Task<List<CategorySuggestionDto>> GetPendingSuggestionsAsync()
    {
        return await _dbContext.CategorySuggestions
            .Include(s => s.Requester)
            .Where(s => s.Status == RequestStatus.Pending)
            .Select(s => new CategorySuggestionDto(s.Id, s.Name, s.Description, s.RequesterId, s.Requester.Username, s.Status.ToString()))
            .ToListAsync();
    }
    public async Task<SuggestionResult> ApproveSuggestionAsync(Guid suggestionId)
    {
        var suggestion = await _dbContext.CategorySuggestions.FindAsync(suggestionId);
        if (suggestion == null)
        {
            return SuggestionResult.NotFound;
        }
        if (suggestion.Status != RequestStatus.Pending)
        {
            return SuggestionResult.NotPending;
        }

        var categoryExists = await _dbContext.Categories.AnyAsync(c => c.Name.ToLower() == suggestion.Name.ToLower());
        if (categoryExists)
        {
            suggestion.Status = RequestStatus.Rejected;
            await _dbContext.SaveChangesAsync();
            return SuggestionResult.AlreadyExists;
        }

        var newCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = suggestion.Name,
            Description = suggestion.Description,
        };

        suggestion.Status = RequestStatus.Approved;
        suggestion.ReviewedAt = DateTime.UtcNow;

        await _dbContext.Categories.AddAsync(newCategory);
        await _dbContext.SaveChangesAsync();

        return SuggestionResult.Success;
    }

    public async Task<SuggestionResult> RejectSuggestionAsync(Guid suggestionId)
    {
        var suggestion = await _dbContext.CategorySuggestions.FindAsync(suggestionId);
        if (suggestion == null)
        {
            return SuggestionResult.NotFound;
        }
        if (suggestion.Status != RequestStatus.Pending)
        {
            return SuggestionResult.NotPending;
        }

        suggestion.Status = RequestStatus.Rejected;
        suggestion.ReviewedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return SuggestionResult.Success;
    }
}
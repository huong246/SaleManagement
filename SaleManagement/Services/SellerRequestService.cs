using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;

namespace SaleManagement.Services;

public class SellerRequestService : ISellerRequestService
{
    private readonly ApiDbContext _dbContext;
    public SellerRequestService(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CreateRequestAsync(Guid userId)
    {
        var existingRequest =
            await _dbContext.SellerUpgradeRequests.AnyAsync(r =>
                r.UserId == userId && r.Status == RequestStatus.Pending);
        if (existingRequest)
        {
            return false;
        }

        var newRequest = new SellerUpgradeRequest()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RequestStatus.Pending
        };
        await _dbContext.SellerUpgradeRequests.AddAsync(newRequest);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    
    public async Task<IEnumerable<SellerUpgradeRequest>> GetPendingRequestsAsync()
    {
        return await _dbContext.SellerUpgradeRequests.Include(r=>r.User).Where(r => r.Status == RequestStatus.Pending).ToListAsync();
    }
    
    public async Task<bool> ApproveRequestAsync(Guid requestId)
    {
        var request = await _dbContext.SellerUpgradeRequests.Include(r=>r.User).FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null|| request.User == null || request.Status != RequestStatus.Pending)
        {
            return false;
        }
        request.User.UserRoles |= UserRole.Seller; 
        request.Status = RequestStatus.Approved;
        request.ReviewedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectRequestAsync(Guid requestId)
    {
        var request = await _dbContext.SellerUpgradeRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null||request.Status != RequestStatus.Pending)
        {
            return false;
        }
        request.Status = RequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
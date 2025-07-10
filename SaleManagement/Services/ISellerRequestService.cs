using SaleManagement.Entities.Enums;

namespace SaleManagement.Services;

public interface ISellerRequestService
{
    
         
        Task<bool> CreateRequestAsync(Guid userId);

        
        Task<IEnumerable<SellerUpgradeRequest>> GetPendingRequestsAsync();

        
        Task<bool> ApproveRequestAsync(Guid requestId);
    
        
        Task<bool> RejectRequestAsync(Guid requestId);
    
}
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class PaymentService :IPaymentService
{
    
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiDbContext _dbContext;
    public PaymentService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }
    
    public async Task<PaymentResult> Payment(PaymentRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return PaymentResult.TokenInvalid;
        }
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return PaymentResult.UserNotFound;
            }

            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId);
            if (order == null)
            {
                return PaymentResult.OrderNotFound;
            }

            if (order.UserId != userId)
            {
                return PaymentResult.OrderNotOwnByUser;
            }

            if (order.Status != OrderStatus.pending)
            {
                return PaymentResult.OrderNotPending;
            }

            if (user.Balance < order.TotalAmount)
            {
                return PaymentResult.BalanceNotEnough;
            }

            //update user balance
            user.Balance -= order.TotalAmount;
            order.Status = OrderStatus.processing;


            await _dbContext.SaveChangesAsync();
            return PaymentResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return PaymentResult.ConcurrencyError;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return PaymentResult.DatabaseError;
        }
        
        
    }
}
using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Hubs;

namespace SaleManagement.Services;

public class PaymentService :IPaymentService
{
    
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    public PaymentService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, IHubContext<NotificationHub> notificationHubContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
        _notificationHubContext = notificationHubContext;
    }

    public async Task<CashInResult> CashIn(CashInRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return CashInResult.TokenInvalid;
        }
        
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
                     return CashInResult.UserNotFound;
        }

        if (request.Amount <= 0)
        {
            return CashInResult.CashInAmountInvalid;
        }
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            user.Balance += request.Amount;
            _dbContext.Users.Update(user);
            var newTransaction = new Transaction()
            {
                UserId = userId,
                Amount = request.Amount,
                Type = TransactionType.CashIn,
                Status = TransactionStatus.Success,
                Note = "Cash In",
            };
            _dbContext.Transactions.Add(newTransaction);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            var userMessage = $"ban da nap thanh cong #{request.Amount} vao tai khoan";
            await _notificationHubContext.Clients.User(userId.ToString()).SendAsync("ReceiveMessage", userMessage);
            return CashInResult.Success;
        }
        catch (DbUpdateException)
        {
            
            await transaction.RollbackAsync();
            return CashInResult.DatabaseError;
        }
        
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
            _dbContext.Users.Update(user);
            _dbContext.Orders.Update(order);

            var buyerTransaction = new Transaction()
            {
                UserId = user.Id,
                Amount = -order.TotalAmount,
                Type = TransactionType.OrderPayment,
                RelatedOrderId = order.Id,
                Status = TransactionStatus.Success,
                Note = $"Order Payment for order {order.Id}",
            };
            _dbContext.Transactions.Add(buyerTransaction);

            var newTransaction = new Transaction()
            {
                UserId = userId,
                Amount = order.TotalAmount,
                Type = TransactionType.OrderPayment,
                RelatedOrderId = order.Id,
                Status = TransactionStatus.Success,
                Note = $"Order Payment for order {order.Id}",
            };
            _dbContext.Transactions.Add(newTransaction);
            foreach (var orderItem in order.OrderItems)
            {
                if (orderItem.Item != null)
                {
                    orderItem.Item.SaleCount += orderItem.Quantity;
                    _dbContext.Items.Update(orderItem.Item);
                }
            }
            var newOrderHistory = new OrderHistory()
            {
                Id = Guid.NewGuid(),
                Order = order,
                OrderId = order.Id,
                Status = OrderStatus.processing,
                Note = "Payment",
                CreatedDate = DateTime.UtcNow,
            };
            _dbContext.OrderHistories.Add(newOrderHistory);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            var userMessage = $"ban da thanh toan thanh cong don hang #{order.Id}";
            await _notificationHubContext.Clients.User(userId.ToString()).SendAsync("ReceiveMessage", userMessage);
            
            var sellerIds = await _dbContext.OrderItems.Where(oi=>oi.OrderId == order.Id).Select(oi=>oi.Item.Shop.UserId).Distinct().ToListAsync();
            foreach (var sellerId in sellerIds)
            {
                var sellerMessage = $"don hang #{order.Id} da duoc thanh toan";
                await _notificationHubContext.Clients.User(sellerId.ToString()).SendAsync("ReceiveMessage", sellerMessage);
            }
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
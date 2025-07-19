using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using Microsoft.AspNetCore.SignalR;
using SaleManagement.Hubs;
using SQLitePCL;

namespace SaleManagement.Services;

public class OrderService : IOrderService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _notificationHubContext; 
    private const decimal shippingFeePerKm = 1000; //phi ship /1km la 1k
    private const double EarthRadiusKm = 6371.0;
    private readonly IShippingService _shippingService;

    public OrderService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, IHubContext<NotificationHub> notificationHubContext, IShippingService shippingService)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
        _notificationHubContext = notificationHubContext;
        _shippingService = shippingService;
    }

    

    public async Task<CreateOrderResult> CreateOrder(CreateOrderRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return CreateOrderResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return CreateOrderResult.UserNotFound;
        }

        var cartItem = await _dbContext.CartItems.Include(ci => ci.Item).Where(ci => ci.UserId == user.Id && request.ItemIds.Contains(ci.ItemId))
            .ToListAsync();
        if (!cartItem.Any())
        {
            return CreateOrderResult.CartIsEmpty;
        }
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var itemIds = cartItem.Select(ci => ci.ItemId).ToList();
            var itemsToUpdate = await _dbContext.Items
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);


            foreach (var CartItem in cartItem)
            {
                if (!itemsToUpdate.TryGetValue(CartItem.ItemId, out var dbItem) || dbItem.Stock < CartItem.Quantity)
                {
                    await transaction.RollbackAsync();
                    return CreateOrderResult.StockNotEnough;
                }

                dbItem.Stock -= CartItem.Quantity;
            }


            var subTotal = cartItem.Sum(ci => ci.Quantity * ci.Item.Price);

            decimal discountProductAmount = 0; //tong tien duoc giam theo voucherProduct
            decimal discountShippingAmount = 0; //tong tien duoc giam theo voucherShipping
            decimal finalTotal = 0; //tong tien cuoi cung 
            var voucherProduct = default(Voucher);
            var voucherShipping = default(Voucher);

            //tinh theo giam gia product
            if (request.VoucherProductId.HasValue)
            {
                voucherProduct = await _dbContext.Vouchers.FindAsync(request.VoucherProductId.Value);
                if (voucherProduct == null || !voucherProduct.IsActive || voucherProduct.Quantity <= 0 ||
                    (voucherProduct.EndDate.HasValue && voucherProduct.EndDate < DateTime.UtcNow))
                {
                    return CreateOrderResult.VoucherExpired;
                }

                if (voucherProduct.MinSpend.HasValue && subTotal < voucherProduct.MinSpend.Value)
                {
                    return CreateOrderResult.MinspendNotMet;
                }

                if (voucherProduct.MethodType == DiscountMethod.Percentage)
                {
                    discountProductAmount = (subTotal * voucherProduct.DiscountValue / 100);
                }
                else
                {
                    discountProductAmount = voucherProduct.DiscountValue;
                }

                //kiem tra xem so tien trong discountProductAmount co vuot muc toi da cua voucher 
                if (voucherProduct.MaxDiscountAmount.HasValue &&
                    discountProductAmount > voucherProduct.MaxDiscountAmount.Value)
                {
                    discountProductAmount = voucherProduct.MaxDiscountAmount.Value;
                }

                //vd voi don 30k nhung duoc giam 50k thi so tien phai tra cx chi la 30k
                if (discountProductAmount > subTotal)
                {
                    discountProductAmount = subTotal;
                }

            }

            finalTotal = subTotal - discountProductAmount;

            //tinh phi ship (theo khoang cach)
            decimal shippingFee = 0; //phi ship

            var itemsByShop = cartItem.GroupBy(ci => ci.Item.ShopId);
            var shopsData = await _dbContext.Shops
                .Where(s => itemsByShop.Select(g => g.Key).Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            foreach (var shopGroup in itemsByShop)
            {
                if (shopsData.TryGetValue(shopGroup.Key, out var currentShop))
                {
                    shippingFee += await _shippingService.CalculateFeeAsync(currentShop, user);
                }
            }

            //tinh giam gia phi ship

            if (request.VoucherShippingId.HasValue)
            {
                voucherShipping = await _dbContext.Vouchers.FindAsync(request.VoucherShippingId.Value);
                if (voucherShipping == null || !voucherShipping.IsActive || voucherShipping.Quantity <= 0 ||
                    (voucherShipping.EndDate.HasValue && voucherShipping.EndDate < DateTime.UtcNow))
                {
                    return CreateOrderResult.VoucherExpired;
                }

                if (voucherShipping.MinSpend.HasValue && subTotal < voucherShipping.MinSpend.Value)
                {
                    return CreateOrderResult.MinspendNotMet;
                }

                if (voucherShipping.MethodType == DiscountMethod.Percentage)
                {
                    discountShippingAmount = (shippingFee * voucherShipping.DiscountValue / 100);
                }
                else
                {
                    discountShippingAmount = voucherShipping.DiscountValue;
                }

                if (discountShippingAmount > shippingFee)
                {
                    discountShippingAmount = shippingFee;
                }
            }

            finalTotal = finalTotal + shippingFee - discountShippingAmount;


            var newOrder = new Order()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                VoucherProductId = request.VoucherProductId,
                VoucherShippingId = request.VoucherShippingId,
                DiscountProductAmount = discountProductAmount,
                DiscountShippingAmount = discountShippingAmount,
                ShippingFee = shippingFee,
                TotalAmount = finalTotal,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.pending,
                SubTotal = cartItem.Sum(ci => ci.Quantity * ci.Item.Price),
            };
            _dbContext.Orders.Add(newOrder);

            var trackingCode = await _shippingService.CreateShippingOrderAsync(newOrder);
            if (!string.IsNullOrEmpty(trackingCode))
            {
                // Cập nhật lại đơn hàng với mã vận đơn
                newOrder.TrackingCode = trackingCode;
                newOrder.ShippingProvider = "GHN";

            }

            var initialOrderHistory = new OrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = newOrder.Id,
                Status = newOrder.Status,
                CreatedDate = DateTime.UtcNow,
                Note = "Don hang duoc tao thanh cong va dang cho xu ly",
                Order = newOrder,
            };
            _dbContext.OrderHistories.Add(initialOrderHistory);



            foreach (var CartItem in cartItem)
            {
                var newOrderItem = new OrderItem()
                {
                    Id = Guid.NewGuid(),
                    OrderId = newOrder.Id,
                    Item = CartItem.Item,
                    ItemId = CartItem.ItemId,
                    Price = CartItem.Item.Price,
                    Quantity = CartItem.Quantity,
                    ShopId = CartItem.Item.ShopId,

                };
                _dbContext.OrderItems.Add(newOrderItem);

            }

            _dbContext.CartItems.RemoveRange(cartItem);
            if (voucherProduct != null)
            {
                if (voucherProduct.Quantity > 0)
                {
                    voucherProduct.Quantity -= 1;
                }


            }

            if (voucherShipping != null)
            {
                if (voucherShipping.Quantity > 0)
                {
                    voucherShipping.Quantity -= 1;
                }


            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            var userMessage = $"Ban da tao thanh cong don hang #{newOrder.Id}";
            await _notificationHubContext.Clients.User(userId.ToString()).SendAsync("ReceiveMessage", userMessage);
            return CreateOrderResult.Success;

        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return CreateOrderResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return CreateOrderResult.DatabaseError;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return CreateOrderResult.DatabaseError;
        }
    }
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Chuyển đổi từ độ sang radian
        var latRad1 = ToRadians(lat1);
        var lonRad1 = ToRadians(lon1);
        var latRad2 = ToRadians(lat2);
        var lonRad2 = ToRadians(lon2);

        // Chênh lệch kinh độ và vĩ độ
        var deltaLat = latRad2 - latRad1;
        var deltaLon = lonRad2 - lonRad1;

        // Áp dụng công thức Haversine
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(latRad1) * Math.Cos(latRad2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        var distance = EarthRadiusKm * c;
        
        return distance;
    }

    // Hàm phụ để chuyển đổi từ độ sang radian.
    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }


    public async Task<UpdateOrderStatusResult> UpdateOrderStatus(UpdateOrderStatusRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UpdateOrderStatusResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return UpdateOrderStatusResult.UserNotFound;
        }
        var order = await _dbContext.Orders.FindAsync(request.OrderId);
        if (order == null)
        {
            return UpdateOrderStatusResult.OrderNotFound;
        }
        
        bool isSellerOfThisOrder = false;
        if (user.UserRoles.HasFlag(UserRole.Seller))
        {
            var userShop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (userShop != null)
            {
                isSellerOfThisOrder = await _dbContext.OrderItems
                    .AnyAsync(oi => oi.OrderId == request.OrderId && oi.ShopId == userShop.Id);
            }
        }
        bool isAdmin = user.UserRoles.HasFlag(UserRole.Admin);

        if (!isAdmin && !isSellerOfThisOrder)
        {
            return UpdateOrderStatusResult.AuthorizeFailed;
        }

        if (!isAdmin)
        {
            var currentStatus = order.Status;
            var newStatus = request.Status;
            var allowedTransitionsForSeller = new Dictionary<OrderStatus, List<OrderStatus>>
            {
                { OrderStatus.pending, new List<OrderStatus> { OrderStatus.processing, OrderStatus.cancelled } },
                { OrderStatus.processing, new List<OrderStatus> { OrderStatus.in_transit } }
            };
            if (!allowedTransitionsForSeller.ContainsKey(currentStatus) || !allowedTransitionsForSeller[currentStatus].Contains(newStatus))
            { 
                return UpdateOrderStatusResult.InvalidStatusTransition;
            }
        }
        

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            order.Status = request.Status;
            _dbContext.Orders.Update(order);

            var newOrderHistory = new OrderHistory()
            {
                Id = Guid.NewGuid(),
                Order = order,
                OrderId = request.OrderId,
                Status = request.Status,
                Note = request.Note,
                CreatedDate = DateTime.UtcNow,
            };
            _dbContext.OrderHistories.Add(newOrderHistory);
        
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            var message = $"Don hang #{order.Id} da duoc cap nhat voi trang thai {request.Status}";
            await _notificationHubContext.Clients.User((order.UserId).ToString()).SendAsync("ReceiveOrderStatusUpdate", message);
            return UpdateOrderStatusResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return UpdateOrderStatusResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return UpdateOrderStatusResult.DatabaseError;
        }
    }

    public async Task<CancelOrderResult> CancelOrder(CancelOrderRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return CancelOrderResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return CancelOrderResult.UserNotFound;
        }

        var order = await _dbContext.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order == null)
        {
            return CancelOrderResult.OrderNotFound;
        }

        if (order.UserId != userId) return CancelOrderResult.AuthorizeFailed;
        if (order.Status != OrderStatus.pending && order.Status != OrderStatus.delivered)
        {
            return CancelOrderResult.NotAllowed;
        }
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
        order.Status = OrderStatus.cancelled;
        foreach (var orderItem in order.OrderItems)
        {
            if (orderItem.Item != null)
            {
                orderItem.Item.Stock += orderItem.Quantity;
            }
        }

        var newOrderHistory = new OrderHistory()
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Status = OrderStatus.cancelled,
            Note = "Cancel order",
            CreatedDate = DateTime.UtcNow,
        };
       
            _dbContext.OrderHistories.Add(newOrderHistory);
            await _dbContext.SaveChangesAsync();
           await transaction.CommitAsync();
           var userMessage = $"ban da huy thanh cong don hang #{order.Id}";
           await _notificationHubContext.Clients.User(userId.ToString()).SendAsync("ReceiveMessage", userMessage);
            return CancelOrderResult.Success;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return CancelOrderResult.DatabaseError;
        }
    }

    public async Task<RequestReturnResult> RequestReturn(RequestReturnRequest request)
    {
       var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
       if (!Guid.TryParse(userIdString, out var userId))
       {
           return RequestReturnResult.TokenInvalid;
       }
       var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
       if (user == null)
       {
           return RequestReturnResult.UserNotFound;
       }
       var order = await _dbContext.Orders.FindAsync(request.OrderId);
       if (order == null)
       {
           return RequestReturnResult.OrderNotFound;
       }

       if (order.UserId != userId)
       {
           return RequestReturnResult.AuthorizeFailed;
       }
       //chi hoan hang khi order da duoc giao thanh cong
       if (order.Status != OrderStatus.delivered)
       {
           return RequestReturnResult.NotAllowed;
       }
       var returnRequest = new ReturnRequest
       {
           OrderId = order.Id,
           Order = order,
           UserId = userId,
           Reason = request.Reason,
           Status = RequestStatus.Pending,
       };
       _dbContext.ReturnRequests.Add(returnRequest);
       try
       {
           await _dbContext.SaveChangesAsync();
           return RequestReturnResult.Success;
       }
       catch (DbUpdateException)
       {
           return RequestReturnResult.DatabaseError;
       }
    }

    public async Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(Guid  orderId)
    {
        return await _dbContext.OrderHistories.Where(o => o.OrderId == orderId).OrderBy(h => h.CreatedDate)
            .ToListAsync();
    }

    public async Task<bool> ProcessPayoutForSuccessfulOrder(ProcessPayoutForSuccessfulOrderRequest request)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var order = await _dbContext.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == request.OrderId);
            if (order == null || order.Status != OrderStatus.completed)
            {
                return false;
            }

            var itemsByShop = order.OrderItems.GroupBy(oi => oi.Item.ShopId);
            foreach (var group in itemsByShop)
            {
                var shopId = group.Key;
                var seller = await _dbContext.Shops.Where(s => s.Id == shopId).Select(s => s.User).FirstOrDefaultAsync();
                if (seller == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
                var amountForShop = group.Sum(oi=>oi.Price * oi.Quantity);
                seller.Balance += amountForShop;
                _dbContext.Users.Update(seller);
                var sellerTransaction = new Transaction()
                {
                    UserId = seller.Id,
                    User = seller,
                    Type = TransactionType.OrderPayment,
                    Status = TransactionStatus.Success,
                    RelatedOrderId = request.OrderId,
                    Amount = amountForShop,
                    Note = $"Order Payment for order {request.OrderId}",
                };
                _dbContext.Transactions.Add(sellerTransaction);
            }
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }
    
    
    
}
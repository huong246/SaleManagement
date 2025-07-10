using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SQLitePCL;

namespace SaleManagement.Services;

public class OrderService : IOrderService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiDbContext _dbContext;
    private const decimal shippingFeePerKm = 1000; //phi ship /1km la 1k
    private const double EarthRadiusKm = 6371.0;

    public OrderService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
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

        var cartItem = await _dbContext.CartItems.Include(ci => ci.Item).Where(ci => ci.UserId == user.Id)
            .ToListAsync();
        if (!cartItem.Any())
        {
            return CreateOrderResult.CartIsEmpty;
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
        var latitude = request.ShippingLatitude ?? user.Latitude;
        var longtitude = request.ShippingLongtitude ?? user.Longitude;
        var itemsByShop = cartItem.GroupBy(ci => ci.Item.ShopId);
        var allShopId = itemsByShop.Select(g => g.Key).ToList();
        var shopsData = await _dbContext.Shops.Where(s => allShopId.Contains(s.Id)).ToDictionaryAsync(s => s.Id);

        foreach (var shopGroup in itemsByShop)
        {
            if (shopsData.TryGetValue(shopGroup.Key, out var currentShop))
            {
                var distance = CalculateDistance(latitude, longtitude, currentShop.Latitude, currentShop.Longitude);
                shippingFee += (decimal) distance * shippingFeePerKm;
            }
            else
            {
                return CreateOrderResult.ShopNotFound;
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

            if (voucherShipping.MinSpend.HasValue && shippingFee < voucherShipping.MinSpend.Value)
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
        
        finalTotal = finalTotal  + shippingFee - discountShippingAmount;
        
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            foreach (var CartItem in cartItem)
            {
                if (CartItem.Item.stock < CartItem.Quantity) //so luong hang goc nho hon so luong hang trong gio
                {
                    await transaction.RollbackAsync();
                    return CreateOrderResult.StockNotEnough;
                }
            }

            var newOrder = new Order()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Latitude = request.ShippingLatitude ?? user.Latitude,
                Longitude = request.ShippingLongtitude ?? user.Longitude,
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

                CartItem.Item.stock -= CartItem.Quantity;
                _dbContext.Items.Update(CartItem.Item);
            }

            _dbContext.CartItems.RemoveRange(cartItem);
            if (voucherProduct != null)
            {
                if (voucherProduct.Quantity > 0)
                {
                    voucherProduct.Quantity -= 1;
                }

                _dbContext.Vouchers.Update(voucherProduct);
            }

            if (voucherShipping != null)
            {
                if (voucherShipping.Quantity > 0)
                {
                    voucherShipping.Quantity -= 1;
                }

                _dbContext.Vouchers.Update(voucherShipping);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
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

        if (!user.UserRoles.HasFlag(UserRole.Admin))
        {
            if (user.UserRoles.HasFlag(UserRole.Seller))
            {
                var userShop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
                var orderItemsFromShop = await _dbContext.OrderItems
                    .AnyAsync(oi => oi.OrderId == request.OrderId && oi.ShopId == userShop.Id);

                if (userShop == null || !orderItemsFromShop)
                {
                    return UpdateOrderStatusResult.AuthorizeFailed;
                }
            }
            else if (user.UserRoles.HasFlag(UserRole.Customer))
            {
                if (order.UserId != userId || request.Status != OrderStatus.pending ||
                    request.Status != OrderStatus.cancelled)
                {
                    return UpdateOrderStatusResult.AuthorizeFailed;
                }
            }
            else
            {
                return UpdateOrderStatusResult.AuthorizeFailed;
            }
            
        }
        
        var currentStatus = order.Status;
        var newStatus = request.Status;
        if (currentStatus == OrderStatus.cancelled || currentStatus == OrderStatus.cancelled ||
            currentStatus == OrderStatus.returned)
        {
            return UpdateOrderStatusResult.InvalidStatusTransition;
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

    public async Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(GetOrderHistoryAsyncRequest request)
    {
        return await _dbContext.OrderHistories.Where(o => o.OrderId == request.OrderId).OrderBy(h => h.CreatedDate)
            .ToListAsync();
    }
}
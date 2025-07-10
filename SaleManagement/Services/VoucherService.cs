using System.Text;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class VoucherService : IVoucherService
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly Random _random = new Random();
    private readonly ApiDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public VoucherService(ApiDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<CreateVoucherResult> CreateVoucher(CreateVoucherRequest request)
    {
         var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
         if (username == null)
         {
             return CreateVoucherResult.TokenInvalid;
         }
         var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Username == username);
         if (user == null)
         {
             return CreateVoucherResult.UserNotFound;
         }

         if (request.Quantity < 0)
         {
             return CreateVoucherResult.QuantityInvalid;
         }

         if (request.ShopId != null)
         {
             var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.Id == request.ShopId);
             if (shop == null)
             {
                 return CreateVoucherResult.ShopNotFound;
             }
         }

         if (request.ItemId != null)
         {
             var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
             if (item == null)
             {
                 return CreateVoucherResult.ItemNotFound;
             }
         }

         var newVoucher = new Voucher();
         newVoucher.Id = Guid.NewGuid();
         bool ktr = false;
         while (ktr == false)
         {
             var newCode = GenerateCode(request.LengthCode);
             bool codeExist = await _dbContext.Vouchers.AnyAsync(v => v.Code == newCode && v.IsActive);
             if (!codeExist)
             {
                 newVoucher.Code = newCode;
                 ktr = true;
             }
         }
         newVoucher.Quantity = request.Quantity;
         newVoucher.ItemId = request.ItemId;
         newVoucher.ShopId = request.ShopId;
         newVoucher.MethodType= request.MethodType;
         newVoucher.TargetType = request.TargetType;
         newVoucher.DiscountValue = request.DiscountValue;
         newVoucher.MinSpend = request.Minspend;
         newVoucher.MaxDiscountAmount = request.MaxDiscountAmount;
         newVoucher.StartDate = request.ValidFrom;
         newVoucher.EndDate = request.ValidUntil;
         if (newVoucher.StartDate >= DateTime.Now && newVoucher.EndDate <= DateTime.Now || newVoucher.Quantity<=0)
         {
             newVoucher.IsActive = false;
         }
         else
         {
             newVoucher.IsActive = request.IsActive;
         }
         
         try {
             _dbContext.Vouchers.Add(newVoucher);
             await _dbContext.SaveChangesAsync();
             return CreateVoucherResult.Success;
         }
         catch (DbUpdateException)
         {
             return CreateVoucherResult.DatabaseError;
         }
        
    }
    public static string GenerateCode(int length)
    {
        var stringBuilder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            stringBuilder.Append(Chars[_random.Next(Chars.Length)]);
        }
        return stringBuilder.ToString();
    }

    public async Task<DeleteVoucherResult> DeleteVoucher(DeleteVoucherRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return DeleteVoucherResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Username == username);
        if (user == null)
        {
            return DeleteVoucherResult.UserNotFound;
        }
        var voucher = await _dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == request.VoucherId);
        if (voucher == null)
        {
            return DeleteVoucherResult.VoucherNotFound;
        }

        if (voucher.ShopId.HasValue)
        {
            var shop = await _dbContext.Shops.FirstOrDefaultAsync(s => s.Id == voucher.ShopId && s.UserId == user.Id);
                    if (shop == null)
                    {
                        return DeleteVoucherResult.ShopNotFound;
                    }
        }
        
        try
        {
            voucher.IsActive = false;
            await _dbContext.SaveChangesAsync();
            return DeleteVoucherResult.Success;
        }
        catch (DbUpdateException)
        {
            return DeleteVoucherResult.DatabaseError;
        }
    }
    public async Task<UpdateVoucherResult> UpdateVoucher(UpdateVoucherRequest request)
    {
        var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return UpdateVoucherResult.TokenInvalid;
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Username == username);
        if (user == null)
        {
            return UpdateVoucherResult.UserNotFound;
        }

        var voucher = await _dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == request.VoucherId);
        if (voucher == null)
        {
            return UpdateVoucherResult.VoucherNotFound;
        }
        _dbContext.Entry(voucher).Property("RowVersion").OriginalValue = request.RowVersion;
        voucher.Quantity = request.Quantity;
        voucher.ItemId = request.ItemId;
        voucher.ShopId = request.ShopId;
        voucher.MethodType= request.MethodType;
        voucher.TargetType = request.TargetType;
        voucher.DiscountValue = request.DiscountValue;
        voucher.MinSpend = request.Minspend;
        voucher.MaxDiscountAmount = request.MaxDiscountAmount;
        voucher.StartDate = request.ValidFrom;
        voucher.EndDate = request.ValidUntil;
        if (voucher.StartDate >= DateTime.Now && voucher.EndDate <= DateTime.Now || voucher.Quantity<=0)
        {
            voucher.IsActive = false;
        }
        else
        {
            voucher.IsActive = request.IsActive;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
            return UpdateVoucherResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return UpdateVoucherResult.ConcurrencyConflict;       
        }
        catch (DbUpdateException)
        {
            return UpdateVoucherResult.DatabaseError;
        }
    }
    
}


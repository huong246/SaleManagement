using System.Security.Claims;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class ItemImageService : IItemImageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiDbContext _dbContext;
    private readonly IWebHostEnvironment _webHostEnvironment;
    
    public ItemImageService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, IWebHostEnvironment webHostEnvironment)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
        _webHostEnvironment = webHostEnvironment;
    }
    public async Task<string> UploadImageAsync(UploadImageRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return string.Empty;
        }
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return string.Empty;
        }

        var item = await _dbContext.Items.FindAsync(request.ItemId);
        if (item == null)
        {
            throw new Exception("Item not found");
        }

        if (request.File == null || request.File.Length == 0)
        {
            return string.Empty;       
        }
        var uploadsFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
        if (!Directory.Exists(uploadsFolderPath))
        {
            Directory.CreateDirectory(uploadsFolderPath);
        }

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
        var filePath = Path.Combine(uploadsFolderPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        var imageUrl = $"/images/{fileName}";
        if (request.IsPrimary)
        {
            var primaryImages = await _dbContext.ItemImages.Where(i => i.ItemId == request.ItemId && i.IsPrimary).ToListAsync();
            foreach (var image in primaryImages)
            {
                image.IsPrimary = false;
            }
        }

        var newItemImage = new ItemImage()
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            Item = item,
            ImageUrl = imageUrl,
            IsPrimary = request.IsPrimary,
        };
        _dbContext.ItemImages.Add(newItemImage);
        await _dbContext.SaveChangesAsync();
        return imageUrl;
    }

    public async Task<DeleteImageResult> DeleteImageAsync(DeleteImageRequest request)
    {
         var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
         if (!Guid.TryParse(userIdString, out var userId))
         {
             return DeleteImageResult.TokenInvalid;
         }
         var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
         if (user == null)
         {
             return DeleteImageResult.UserNotFound;
         }

         var image = await _dbContext.ItemImages.Include(i=>i.Item).ThenInclude(item => item.Shop).ThenInclude(shop => shop.User).FirstOrDefaultAsync(i => i.Id == request.ImageId);
         if (image == null)
         {
             return DeleteImageResult.ImageNotFound;
         }
         try
         {
         var relativePath = image.ImageUrl.TrimStart('/');
         var filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
         if (File.Exists(filePath))
         {
             File.Delete(filePath);
         }
             
             _dbContext.ItemImages.Remove(image);
             await _dbContext.SaveChangesAsync();
             return DeleteImageResult.Success;
         }
         catch (DbUpdateException)
         {
              return DeleteImageResult.DatabaseError;
         }
    }
}
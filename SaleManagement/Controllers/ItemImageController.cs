using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize (Roles = $"{nameof(UserRole.Seller)}")]
public class ItemImageController : ControllerBase
{
    private readonly IItemImageService _itemImageService;

    [HttpPost("upload_image")]
    public async Task<IActionResult> UploadImage(UploadImageRequest request)
    {
        var imageUrl = await _itemImageService.UploadImageAsync(request);
        return Ok(imageUrl);
    }

    [HttpDelete("{imageId}")]
    public async Task<IActionResult> DeleteImage(DeleteImageRequest request)
    {
       var result = await _itemImageService.DeleteImageAsync(request);
       return result switch
       {
           DeleteImageResult.Success => Ok("Image deleted successfully"),
           DeleteImageResult.TokenInvalid => Unauthorized("Token is invalid"),
           DeleteImageResult.UserNotFound => NotFound("User not found"),
           DeleteImageResult.ImageNotFound => NotFound("Image not found"),
           _ => StatusCode(500, "An unexpected error occurred while deleting the image")
       };
    }
}
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum DeleteImageResult
{
    Success,
    DatabaseError,
    ImageNotFound,
    TokenInvalid,
    UserNotFound,
}
 
public interface IItemImageService
{
    Task<string> UploadImageAsync(UploadImageRequest request);
    Task<DeleteImageResult> DeleteImageAsync(DeleteImageRequest request);
 
}
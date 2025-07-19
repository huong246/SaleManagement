namespace SaleManagement.Schemas;

public record UploadImageRequest(Guid ItemId, IFormFile File, bool IsPrimary);
public record DeleteImageRequest(Guid ImageId);

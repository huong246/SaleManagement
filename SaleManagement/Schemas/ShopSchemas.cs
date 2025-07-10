namespace SaleManagement.Schemas;

public record CreateShopRequest(string Name, double Latitude, double Longitude, int PreparationTime);
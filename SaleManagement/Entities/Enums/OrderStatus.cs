namespace SaleManagement.Entities.Enums;

public enum OrderStatus
{
    pending,
    processing,
    completed,
    in_transit, //dang tren duong giap
    cancelled,
    delivered,
    returned,
}
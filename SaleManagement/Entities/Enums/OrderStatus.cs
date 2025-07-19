namespace SaleManagement.Entities.Enums;

public enum OrderStatus
{
    pending,
    processing,
    in_transit,//dang tren duong giao
    delivered,
    completed,
    cancelled,
    returned,
}
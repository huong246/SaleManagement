using SaleManagement.Entities;

namespace SaleManagement.Schemas;

public record FeeRequest(int service_id, int insurance_value, string coupon, int to_ward_code, string to_district_id, int from_district_id, int weight, int length, int width, int height);
public record CreateRequest(string to_name, string to_phone, string to_address, string to_ward_code, int to_district_id, decimal required_note, int weight, int length, int width, int height, List<OrderItem> items, int service_id, int payment_type_id, string note);
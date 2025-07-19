using System.Text.Json;
using SaleManagement.Data;
using SaleManagement.Entities;

namespace SaleManagement.Services;

public class ShippingService : IShippingService
{
    
    private readonly ApiDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    public ShippingService(IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_configuration["Ghn:ApiUrl"]);
        _httpClient.DefaultRequestHeaders.Add("Token", _configuration["Ghn:Token"]);
        _httpClient.DefaultRequestHeaders.Add("ShopId", _configuration["Ghn:ShopId"]);
    }
    public async Task<decimal> CalculateFeeAsync(Shop shop, User user)
    {
        var requestData = new {
            from_district_id = 1442,
            service_id = 53320,
            to_district_id = 1443,
            to_ward_code = "20314",
            weight = 200,
            insurance_value = 500000
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("shipping-order/fee", requestData);
            
            response.EnsureSuccessStatusCode(); 

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            if (jsonResponse.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("total", out var totalElement) &&
                totalElement.ValueKind == JsonValueKind.Number)
            {
                return totalElement.GetDecimal();
            }
            
            throw new InvalidOperationException("Cấu trúc JSON trả về từ API vận chuyển không hợp lệ.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Không thể kết nối đến dịch vụ vận chuyển.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Dữ liệu trả về từ API vận chuyển không phải là JSON hợp lệ.", ex);
        }
    }

    public async Task<string> CreateShippingOrderAsync(Order order)
    {
        var requestData = new {
            to_name = order.User.Username,  
            to_phone = "0355813460",  
            to_address = "...",
            to_ward_code = "20314",
            to_district_id = 1443,
         
            payment_type_id = 2,  
            note= "Vui lòng gọi trước khi giao",
            required_note = "CHOXEMHANG",
            items = order.OrderItems.Select(oi => new { name = oi.Item.Name, quantity = oi.Quantity, price = (int)oi.Price }).ToList(),
            weight = 500
        };

        var response = await _httpClient.PostAsJsonAsync("shipping-order/create", requestData);
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        return jsonResponse.GetProperty("data").GetProperty("order_code").GetString();
    }
}
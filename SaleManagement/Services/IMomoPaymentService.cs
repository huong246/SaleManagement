using System.Text.Json;
using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public enum IpnProcessResult
{
    Success,
    InvalidSignature,
    OrderNotFound,
    OrderAlreadyProcessed,
    Error
}

public interface IPaymentGateWayService
{
    Task<MomoPaymentResponse> CreateMomoPaymentAsync(Order order);
    Task<IpnProcessResult> ProcessIpnResponseAsync(JsonElement body);
    bool ValidateSignature(string rawData, string signature);
}
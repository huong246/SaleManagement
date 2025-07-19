using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;

namespace SaleManagement.Services;


public class MomoService : IMomoPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ApiDbContext _dbContext;


    public MomoService(IConfiguration configuration, ApiDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
     
    }

    public async Task<MomoPaymentResponse> CreateMomoPaymentAsync(Order order)
    {
        var config = _configuration.GetSection("Momo");
        var partnerCode = config["PartnerCode"];
        var accessKey = config["AccessKey"];
        var secretKey = config["SecretKey"];
        var endpoint = config["ApiEndpoint"];
        var returnUrl = config["ReturnUrl"];
        var notifyUrl = config["NotifyUrl"];

        var requestId = Guid.NewGuid().ToString();
        var orderId = order.Id.ToString();
        var amount = (long)order.TotalAmount;
        var orderInfo = $"Thanh toán đơn hàng {order.Id}";
        var requestType = "captureWallet";

        var rawHash = $"accessKey={accessKey}&amount={amount}&extraData=&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";

        var signature = SignHmacSHA256(rawHash, secretKey);

        var requestBody = new {
            partnerCode,
            accessKey,
            requestId,
            amount,
            orderId,
            orderInfo,
            redirectUrl = returnUrl,
            ipnUrl = notifyUrl,
            requestType,
            extraData = "",
            lang = "vi",
            signature
        };

        using var client = new HttpClient();
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        requestMessage.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<MomoPaymentResponse>(responseContent);
    }
    
    private string SignHmacSHA256(string message, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    public bool ValidateSignature(string rawData, string signature)
    {
        var secretKey = _configuration["Momo:SecretKey"];
        var mySignature = SignHmacSHA256(rawData, secretKey);
        return mySignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }
    public async Task<IpnProcessResult> ProcessIpnResponseAsync(JsonElement body)
        {
            var ipnData = JsonConvert.DeserializeObject<Dictionary<string, object>>(body.ToString());
            if (ipnData == null)
            {
                return IpnProcessResult.Error;
            }

            // Lấy dữ liệu từ IPN
            var partnerCode = ipnData.GetValueOrDefault("partnerCode")?.ToString();
            var orderIdStr = ipnData.GetValueOrDefault("orderId")?.ToString();
            var requestId = ipnData.GetValueOrDefault("requestId")?.ToString();
            var amount = ipnData.GetValueOrDefault("amount")?.ToString();
            var orderInfo = ipnData.GetValueOrDefault("orderInfo")?.ToString();
            var orderType = ipnData.GetValueOrDefault("orderType")?.ToString();
            var transId = ipnData.GetValueOrDefault("transId")?.ToString();
            var resultCode = ipnData.GetValueOrDefault("resultCode")?.ToString();
            var message = ipnData.GetValueOrDefault("message")?.ToString();
            var payType = ipnData.GetValueOrDefault("payType")?.ToString();
            var responseTime = ipnData.GetValueOrDefault("responseTime")?.ToString();
            var extraData = ipnData.GetValueOrDefault("extraData")?.ToString() ?? "";
            var momoSignature = ipnData.GetValueOrDefault("signature")?.ToString();
            
            // Xác thực chữ ký
            var rawData = $"partnerCode={partnerCode}&accessKey={_configuration["Momo:AccessKey"]}&requestId={requestId}&amount={amount}&orderId={orderIdStr}&orderInfo={orderInfo}&orderType={orderType}&transId={transId}&message={message}&localMessage={message}&responseTime={responseTime}&errorCode={resultCode}&payType={payType}&extraData={extraData}";
            var isValidSignature = ValidateSignature(rawData, momoSignature);

            if (!isValidSignature)
            {
                return IpnProcessResult.InvalidSignature;
            }

            // Xử lý logic nếu thanh toán thành công
            if (resultCode == "0")
            {
                if (Guid.TryParse(orderIdStr, out var orderId))
                {
                    var order = await _dbContext.Orders.FindAsync(orderId);
                    if (order == null)
                    {
                        return IpnProcessResult.OrderNotFound;
                    }

                    if (order.Status != OrderStatus.pending)
                    {
                        return IpnProcessResult.OrderAlreadyProcessed; // Đơn hàng đã được xử lý trước đó
                    }

                    order.Status = OrderStatus.processing;

                    var transaction = new Transaction
                    {
                        UserId = order.UserId,
                        Amount = order.TotalAmount,
                        Type = TransactionType.OrderPayment,
                        RelatedOrderId = order.Id,
                        Status = Entities.TransactionStatus.Success,
                        Note = $"MoMo Payment. Transaction ID: {transId}"
                    };
                    _dbContext.Transactions.Add(transaction);

                    _dbContext.OrderHistories.Add(new OrderHistory
                    {
                        OrderId = orderId,
                        Status = OrderStatus.processing,
                        Note = "Payment completed via MoMo."
                    });

                    await _dbContext.SaveChangesAsync();
                    return IpnProcessResult.Success;
                }
            }

            return IpnProcessResult.Error; // Hoặc một trạng thái lỗi khác phù hợp
        }
}

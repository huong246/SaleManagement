using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;

namespace SaleManagement.Services;

public class VoucherUpdateStatusService : BackgroundService
{
    
    private readonly ILogger<VoucherUpdateStatusService> _logger;

    private readonly IServiceProvider _serviceProvider;

    public VoucherUpdateStatusService(IServiceProvider serviceProvider, ILogger<VoucherUpdateStatusService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Voucher Status Updater Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeactivateExpiredVouchers();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating voucher statuses.");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("Voucher Status Updater Service is stopping.");
    }

    private async Task DeactivateExpiredVouchers()
    {
        _logger.LogInformation("Running job to deactivate expired or out-of-stock vouchers.");
        
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

            var now = DateTime.UtcNow;

           
            var vouchersToUpdate = await dbContext.Vouchers
                .Where(v => v.IsActive && (v.EndDate < now || v.Quantity <= 0))
                .ToListAsync();

            if (vouchersToUpdate.Any())
            {
                foreach (var voucher in vouchersToUpdate)
                {
                    voucher.IsActive = false;
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Successfully deactivated {vouchersToUpdate.Count} vouchers.");
            }
            else
            {
                _logger.LogInformation("No vouchers needed deactivation at this time.");
            }
        }
    }
}
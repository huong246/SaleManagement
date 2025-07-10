using System.ComponentModel.DataAnnotations;
using SaleManagement.Entities.Enums;

namespace SaleManagement.Entities;

public class Voucher
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid? ShopId { get; set; }
    public Guid? ItemId { get; set; }
    public decimal DiscountValue { get; set; } //tinh theo methodtype, với số 20, với percentage thì 20% còn với fixedamount thì l 20k 
    
    public VoucherTarger TargetType { get; set; } //theo product hay shipping
    
    public DiscountMethod MethodType { get; set; } //theo phan tram hay so tien co dinh
    public int Quantity { get; set; } 
    public decimal? MinSpend { get; set; } //so tien toi thieu de ap dung voucher
    public decimal? MaxDiscountAmount { get; set; } //so tien giam toi da
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
using SaleManagement.Entities.Enums;

namespace SaleManagement.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid? VoucherProductId { get; set; }
    public Guid? VoucherShippingId { get; set; }
    public decimal? DiscountProductAmount { get; set; }  //tong tien duoc giam theo voucherProduct
    public decimal? DiscountShippingAmount { get; set; } //tong tien duoc giam theo voucherShipping
    public decimal SubTotal { get; set; } //tong tien hang 
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; } //tong tien hoa don cuoi cung
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; }
    public Order()
    {
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.pending;
        OrderItems = new HashSet<OrderItem>();
    }
}
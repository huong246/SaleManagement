using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;

public enum TransactionType { CashIn, OrderPayment }
public enum TransactionStatus { Success, Failed }

[Table("Transactions")]
public class Transaction
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public virtual User User { get; set; }

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public Guid? RelatedOrderId { get; set; }  

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public TransactionStatus Status { get; set; }

    public string? Note { get; set; }
}
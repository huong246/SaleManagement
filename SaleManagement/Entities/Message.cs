using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;

[Table("Messages")]
public class Message
{
    [Key]
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public virtual Conversation Conversation { get; set; }
    public Guid SenderId { get; set; } // ID người gửi
    public string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
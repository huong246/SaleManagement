using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;

[Table("Conversations")]
public class Conversation
{
    [Key]
    public Guid Id { get; set; }
    public Guid ParticipantA_Id { get; set; }
    public Guid ParticipantB_Id { get; set; }
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
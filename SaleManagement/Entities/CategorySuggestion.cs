using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SaleManagement.Entities.Enums;

namespace SaleManagement.Entities;

[Table("CategorySuggestions")]
public class CategorySuggestion
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string? Description { get; set; }
    [Required]
    public Guid RequesterId { get; set; }
    
    [ForeignKey("RequesterId")]
    public virtual User? Requester { get; set; }
    public RequestStatus Status { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public CategorySuggestion()
    {
        RequestedAt = DateTime.UtcNow;
        Status = RequestStatus.Pending;
    }
}
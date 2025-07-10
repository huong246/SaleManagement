using Microsoft.AspNetCore.Mvc;

namespace SaleManagement.Entities;

public class ReviewAndRating 
{
    public Guid Id { get; set; }
    public int Rating { get; set; } //1->5
    public string? Comment { get; set; }
    public DateTime CreatedReviewDate { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    
}
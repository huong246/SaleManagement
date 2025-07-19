using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;

namespace SaleManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ApiDbContext _dbContext;
    public ChatController(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("get_chat")]
    public async Task<IActionResult> GetChatHistory(Guid ortherUserId)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var messages = await _dbContext.Messages
            .Where(m =>
                (m.Conversation.ParticipantA_Id == currentUserId && m.Conversation.ParticipantB_Id == ortherUserId) ||
                (m.Conversation.ParticipantA_Id == ortherUserId && m.Conversation.ParticipantB_Id == currentUserId))
            .OrderBy(m => m.Timestamp).Select(m => new { m.SenderId, m.Content, m.Timestamp }).ToListAsync();
        return Ok(messages);
    }
    
}
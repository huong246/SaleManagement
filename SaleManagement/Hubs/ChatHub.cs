using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;

namespace SaleManagement.Hubs;
[Authorize]
public class ChatHub : Hub
{
    private readonly ApiDbContext _dbContext;
    public ChatHub(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SendMessage(string receiverId, string messageContent)
    {
        var senderIdStr = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(senderIdStr, out var senderId) || !Guid.TryParse(receiverId, out var receiverGuid))
        {
            return;
        }
        var conversation = await _dbContext.Conversations.FirstOrDefaultAsync(c=> (c.ParticipantA_Id == senderId && c.ParticipantB_Id == receiverGuid) || (c.ParticipantA_Id == receiverGuid && c.ParticipantB_Id == senderId));
        if (conversation == null)
        {
            conversation = new Conversation()
            {
                ParticipantA_Id = senderId,
                ParticipantB_Id = receiverGuid
            };
            _dbContext.Conversations.Add(conversation);
        }
        var message = new Message()
        {
            Conversation = conversation,
            Content = messageContent,
            SenderId = senderId,
            ConversationId = conversation.Id,
            Timestamp = DateTime.UtcNow,
            IsRead = false,
        };
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();
        await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId.ToString(), messageContent);
        await Clients.User(senderIdStr).SendAsync("ReceiveMessage", receiverId, messageContent);
        
    }
    
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
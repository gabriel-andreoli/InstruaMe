using InstruaMe.Domain.Entities;
using InstruaMe.Domain.Models.Commands;
using InstruaMe.Domain.Models.Results;
using InstruaMe.Infrastructure.ORM;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace InstruaMe.Services
{
    public class ChatWebSocketHandler
    {
        private readonly WebSocketManager _wsManager;
        private readonly IServiceScopeFactory _scopeFactory;

        public ChatWebSocketHandler(WebSocketManager wsManager, IServiceScopeFactory scopeFactory)
        {
            _wsManager = wsManager;
            _scopeFactory = scopeFactory;
        }

        public async Task HandleAsync(Guid conversationId, Guid userId, string role, WebSocket socket, CancellationToken ct)
        {
            _wsManager.AddSocket(conversationId, userId, socket);

            var buffer = new byte[4096];

            try
            {
                while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.MessageType != WebSocketMessageType.Text)
                        continue;

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    SendChatMessageCommand command;
                    try
                    {
                        command = JsonSerializer.Deserialize<SendChatMessageCommand>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                    }
                    catch
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(command?.Content))
                        continue;

                    ChatMessageResult messageResult;

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<InstruaMeDbContext>();

                        var conversation = await db.Conversations
                            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

                        if (conversation is null ||
                            (conversation.InstructorId != userId && conversation.StudentId != userId))
                            break;

                        var message = new ChatMessage(conversationId, userId, role, command.Content);
                        db.ChatMessages.Add(message);
                        await db.SaveChangesAsync(ct);

                        messageResult = new ChatMessageResult
                        {
                            Id = message.Id,
                            ConversationId = message.ConversationId,
                            SenderId = message.SenderId,
                            SenderRole = message.SenderRole,
                            Content = message.Content,
                            Read = message.Read,
                            CreatedAt = message.CreatedAt
                        };
                    }

                    var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageResult));
                    var segment = new ArraySegment<byte>(responseBytes);

                    // Echo para o sender
                    if (socket.State == WebSocketState.Open)
                        await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);

                    // Broadcast para os outros participantes
                    foreach (var participant in _wsManager.GetParticipants(conversationId, userId))
                    {
                        if (participant.State == WebSocketState.Open)
                            await participant.SendAsync(segment, WebSocketMessageType.Text, true, ct);
                    }
                }
            }
            finally
            {
                _wsManager.RemoveSocket(conversationId, userId);

                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None); }
                    catch { /* ignorar */ }
                }
            }
        }
    }
}

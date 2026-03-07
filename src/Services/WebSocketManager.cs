using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace InstruaMe.Services
{
    public class WebSocketManager
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, WebSocket>> _sockets = new();

        public void AddSocket(Guid conversationId, Guid userId, WebSocket socket)
        {
            var conversation = _sockets.GetOrAdd(conversationId, _ => new ConcurrentDictionary<Guid, WebSocket>());
            conversation[userId] = socket;
        }

        public void RemoveSocket(Guid conversationId, Guid userId)
        {
            if (_sockets.TryGetValue(conversationId, out var conversation))
            {
                conversation.TryRemove(userId, out _);
                if (conversation.IsEmpty)
                    _sockets.TryRemove(conversationId, out _);
            }
        }

        public IEnumerable<WebSocket> GetParticipants(Guid conversationId, Guid excludeUserId)
        {
            if (_sockets.TryGetValue(conversationId, out var conversation))
            {
                foreach (var kv in conversation)
                {
                    if (kv.Key != excludeUserId && kv.Value.State == WebSocketState.Open)
                        yield return kv.Value;
                }
            }
        }
    }
}

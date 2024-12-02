using StackExchange.Redis;
using System.Net.WebSockets;
using System.Text;

namespace OTRPO_Lab9
{
    public class ChatHandler
    {
        private readonly ChatService _chatService;
        private readonly ISubscriber _subscriber;

        public ChatHandler(ChatService chatService, ISubscriber subscriber)
        {
            _chatService = chatService;
            _subscriber = subscriber;
        }

        public async Task HandleAsync(HttpContext context, WebSocket webSocket)
        {
            const string CHANNEL = "chat";
            var userId = Guid.NewGuid().ToString();
            var username = _chatService.GenerateUsername();

            // Добавляем пользователя в онлайн и отправляем список
            await _chatService.AddUserAsync(userId, username);
            var onlineUsers = await _chatService.GetOnlineUsersAsync();
            await _subscriber.PublishAsync(CHANNEL, $"ONLINE_USERS:{string.Join(",", onlineUsers.Values)}");

            // Отправляем историю сообщений
            var history = await _chatService.GetMessageHistoryAsync();
            foreach (var msg in history)
            {
                await SendMessageAsync(webSocket, msg);
            }

            // Подписываемся на новые сообщения
            _ = Task.Run(() => _subscriber.SubscribeAsync(CHANNEL, async (_, message) =>
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await SendMessageAsync(webSocket, message);
                }
            }));

            // Обрабатываем входящие сообщения
            var buffer = new byte[1024 * 4];
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await _chatService.SaveMessageAsync(username, message);
                        await _subscriber.PublishAsync(CHANNEL, $"{username}: {message}");
                    }
                }
            }
            finally
            {
                await _chatService.RemoveUserAsync(userId);
                onlineUsers = await _chatService.GetOnlineUsersAsync();
                await _subscriber.PublishAsync(CHANNEL, $"ONLINE_USERS:{string.Join(",", onlineUsers.Values)}");
            }
        }

        private async Task SendMessageAsync(WebSocket socket, string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

}

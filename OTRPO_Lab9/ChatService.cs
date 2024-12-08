namespace OTRPO_Lab9
{
    using OTRPO_Lab9.Models;
    using StackExchange.Redis;
    using System.Text.Json;

    public class ChatService
    {
        private const string MESSAGE_HISTORY_KEY = "chat:history";
        private const string ONLINE_USERS_KEY = "chat:online_users";        
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        public ChatService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _database = _redis.GetDatabase();
        }

        // Сохранение сообщения в истории
        public async Task SaveMessageAsync(string username, string message)
        {
            var fullMessage = $"{username}: {message}";
            await _database.ListLeftPushAsync(MESSAGE_HISTORY_KEY, fullMessage);
            await _database.ListTrimAsync(MESSAGE_HISTORY_KEY, 0, 99); // Храним только последние 100 сообщений
        }

        // Получение истории сообщений
        public async Task<IEnumerable<string>> GetMessageHistoryAsync()
        {            
            var messages = await _database.ListRangeAsync(MESSAGE_HISTORY_KEY, 0, -1);
            return messages.Reverse().Select(m => m.ToString());
        }

        // Добавление пользователя в список онлайн
        public async Task AddUserAsync(string userId, string username)
        {            
            await _database.HashSetAsync(ONLINE_USERS_KEY, userId, username);
        }

        // Удаление пользователя из списка онлайн
        public async Task RemoveUserAsync(string userId)
        {            
            await _database.HashDeleteAsync(ONLINE_USERS_KEY, userId);
        }

        // Получение списка онлайн-пользователей
        public async Task<Dictionary<string, string>> GetOnlineUsersAsync()
        {            
            var users = await _database.HashGetAllAsync(ONLINE_USERS_KEY);
            return users.ToDictionary(
                entry => entry.Name.ToString(),
                entry => entry.Value.ToString()
            );
        }

        public async Task<string> CreatePrivateRoomAsync(string user1, string user2)
        {
            var roomId = Guid.NewGuid().ToString(); // Уникальный идентификатор комнаты
            var room = new PrivateRoom
            {
                RoomId = roomId,
                Participants = new List<string> { user1, user2 }
            };

            // Сохраняем комнату в Redis
            var roomJson = JsonSerializer.Serialize(room);
            await _database.HashSetAsync("private_rooms", roomId, roomJson);

            return roomId;
        }

        public async Task<PrivateRoom> GetPrivateRoomAsync(string roomId)
        {
            var roomJson = await _database.HashGetAsync("private_rooms", roomId);
            return roomJson.IsNullOrEmpty
                ? null
                : JsonSerializer.Deserialize<PrivateRoom>(roomJson);
        }

        public async Task SavePrivateRoomAsync(string roomId, PrivateRoom room)
        {
            var roomJson = JsonSerializer.Serialize(room);
            await _database.HashSetAsync($"private_rooms", roomId, roomJson);
        }


    }


}

namespace OTRPO_Lab9
{
    using StackExchange.Redis;

    public class ChatService
    {
        private const string MESSAGE_HISTORY_KEY = "chat:history";
        private const string ONLINE_USERS_KEY = "chat:online_users";
        private readonly IConnectionMultiplexer _redis;

        public ChatService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public string GenerateUsername()
        {
            var adjectives = new[] { "Happy", "Clever", "Brave", "Bright", "Witty" };
            var animals = new[] { "Lion", "Eagle", "Fox", "Panda", "Wolf" };
            var random = new Random();
            return $"{adjectives[random.Next(adjectives.Length)]} {animals[random.Next(animals.Length)]}";
        }

        // Сохранение сообщения в истории
        public async Task SaveMessageAsync(string username, string message)
        {
            var db = _redis.GetDatabase();
            var fullMessage = $"{username}: {message}";
            await db.ListLeftPushAsync(MESSAGE_HISTORY_KEY, fullMessage);
            await db.ListTrimAsync(MESSAGE_HISTORY_KEY, 0, 99); // Храним только последние 100 сообщений
        }

        // Получение истории сообщений
        public async Task<IEnumerable<string>> GetMessageHistoryAsync()
        {
            var db = _redis.GetDatabase();
            var messages = await db.ListRangeAsync(MESSAGE_HISTORY_KEY, 0, -1);
            return messages.Reverse().Select(m => m.ToString());
        }

        // Добавление пользователя в список онлайн
        public async Task AddUserAsync(string userId, string username)
        {
            var db = _redis.GetDatabase();
            await db.HashSetAsync(ONLINE_USERS_KEY, userId, username);
        }

        // Удаление пользователя из списка онлайн
        public async Task RemoveUserAsync(string userId)
        {
            var db = _redis.GetDatabase();
            await db.HashDeleteAsync(ONLINE_USERS_KEY, userId);
        }

        // Получение списка онлайн-пользователей
        public async Task<Dictionary<string, string>> GetOnlineUsersAsync()
        {
            var db = _redis.GetDatabase();
            var users = await db.HashGetAllAsync(ONLINE_USERS_KEY);
            return users.ToDictionary(
                entry => entry.Name.ToString(),
                entry => entry.Value.ToString()
            );
        }
    }

}

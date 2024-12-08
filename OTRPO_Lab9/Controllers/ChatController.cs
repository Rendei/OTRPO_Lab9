using Microsoft.AspNetCore.Mvc;

namespace OTRPO_Lab9.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;
        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("create-room")]
        public async Task<IActionResult> CreatePrivateRoom([FromQuery] string user1, [FromQuery] string user2)
        {
            if (string.IsNullOrWhiteSpace(user1) || string.IsNullOrWhiteSpace(user2))
            {
                return BadRequest("Both usernames are required.");
            }

            var roomId = await _chatService.CreatePrivateRoomAsync(user1, user2);
            return Ok(new { RoomId = roomId });
        }

    }
}

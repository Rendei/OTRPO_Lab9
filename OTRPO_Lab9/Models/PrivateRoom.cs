namespace OTRPO_Lab9.Models
{
    public class PrivateRoom
    {
        public string RoomId { get; set; }
        public List<string> Participants { get; set; } = new List<string>();
    }
}

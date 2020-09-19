namespace PayoutBot.Discord.Models
{
    public class Player
    {
        public string Name { get; set; }
        public string Flag { get; set; }
        public string ProfileUrl { get; set; }
        public int PayoutHour { get; set; } // in UTC
    }
}
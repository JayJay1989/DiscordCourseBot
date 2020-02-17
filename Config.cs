namespace DiscordBot
{
    public class Config
    {
        public Bot bot { get; set; }
        public int minutesToRefresh { get; set; }
    }

    public class Bot
    {
        public string token { get; set; }
    }
}

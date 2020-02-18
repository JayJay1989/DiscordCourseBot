using Discord.Rest;

namespace DiscordBot.Models.Messages
{
    public class Message
    {
        public RestUserMessage ThisMessage { get; protected set; }

        public Message(RestUserMessage message)
        {
            this.ThisMessage = message;
        }
    }
}

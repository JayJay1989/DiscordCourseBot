using System;

namespace DiscordBot.Models
{
    public class Tasks : IEquatable<Tasks>
    {
        public string Course { get; set; }
        public string Title { get; set; }
        public DateTime EndTime { get; set; }

        public bool Equals(Tasks other)
        {
            return this.EndTime.Equals(other.EndTime) && 
                   this.Title.Equals(other.Title) &&
                   this.Course.Equals(other.Course);
        }
    }
}

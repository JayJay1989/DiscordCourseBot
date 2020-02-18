using System;

namespace DiscordBot.Models
{
    public class TimeTable
    {
        public DateTime Day { get; set; }
        public string Subject { get; set; }
        public string ClassRoom { get; set; }
        public string Teacher { get; set; }
        public Time Time { get; set; }
    }

    public class Time
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}

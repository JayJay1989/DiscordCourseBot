using System;
using System.Collections.Generic;
using DiscordBot.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Microsoft.Extensions.Configuration;

namespace DiscordBot
{
    public class ICSDownloader
    {
        private List<Tasks> taskList;

        public ICSDownloader()
        {
            taskList = new List<Tasks>();
            IConfiguration _config = Program.ThisProgram.GetConfig();
            Calendar calendar = new Calendar().LoadFromUri(new Uri(_config["icalUrlTasks"]));
            foreach (CalendarEvent calendarEvent in calendar.Events)
            {
                taskList.Add(new Tasks
                {
                    Course = GetSummary(calendarEvent.Summary, 1), 
                    Title = GetSummary(calendarEvent.Summary, 0), 
                    EndTime = calendarEvent.End.Value
                });
            }
        }

        public List<Tasks> GetTaskList()
        {
            return taskList;
        }

        private string GetSummary(string haystack, int i)
        {
            string[] splitted = haystack.Replace("]", "").Split(" [");
            return splitted[i];
        }
    }
}

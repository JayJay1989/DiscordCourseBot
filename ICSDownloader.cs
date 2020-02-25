using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            Task<Calendar> calendar = new Calendar().LoadFromUriAsync(new Uri(_config["icalUrlTasks"]));
            
            if (calendar == null)
            {
                Program.ThisProgram.Logger.Debug($"Failed to get calendar, got: {calendar}");
                return;
            }
            foreach (CalendarEvent calendarEvent in calendar.Result.Events)
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
            return splitted.Length < i+1 ? "" : splitted[i];
        }
    }
}

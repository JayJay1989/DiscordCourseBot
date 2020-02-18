using System;
using System.Collections.Generic;
using System.IO;
using DiscordBot.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;

namespace DiscordBot
{
    public class ICSConverter
    {
        private List<TimeTable> timetable;

        public ICSConverter()
        {
            timetable = new List<TimeTable>();
            Calendar callenderCollection = Calendar.Load(File.ReadAllText($"{Environment.CurrentDirectory}/calendar.ics"));

            foreach (CalendarEvent calendar in callenderCollection.Events)
            {
                timetable.Add(new TimeTable
                {
                    ClassRoom = calendar.Location, 
                    Day = GetDateZeroTime(calendar.DtStart.Value),
                    Subject = calendar.Summary,
                    Teacher = GetTeacher(calendar.Description),
                    Time = new Time
                    {
                        StartTime = calendar.DtStart.Value,
                        EndTime = calendar.DtEnd.Value
                    }
                });
            }
        }

        private DateTime GetDateZeroTime(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
        }

        private string GetTeacher(string haystack)
        {
            string[] ret = haystack.Replace("\n\n", "\n").Split("\n");
            if (ret.Length < 4) return "";
            return ret[3].Replace("Lector(en): ", "");
        }

        public List<TimeTable> GetTables()
        {
            return timetable;
        }
    }
}

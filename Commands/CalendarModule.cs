﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordBot.Models;
using Tababular;

namespace DiscordBot.Commands
{
    public class CalendarModule : ModuleBase<SocketCommandContext>
    {

        [Command("calendar")]
        [Summary("Get calendar.")]
        public async Task SendCalendarAsync([Summary("The (optional) weeknumber")] string weeknumber = null)
        {
            TableFormatter formatter = new TableFormatter();
            IEnumerable<TimeTable> timeTable = Program.ThisProgram.GetWeekTable(weeknumber);
            string[] weekdays = {"Zondag", "Maandag", "Dinsdag", "Woensdag", "Donderdag", "Vrijdag"};
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            Dictionary<string, object> newItem = new Dictionary<string, object>();
            newItem.Add("-", new[]{"VM","NM"});
            for (int i = 1; i <= 5; i++)
            {
                IEnumerable<TimeTable> thisTimeTable = timeTable.Where(tt => (int) tt.Day.DayOfWeek == i);
                string today = (i == (int)DateTime.Today.DayOfWeek && weeknumber == null) ? "(*)" : "";
                if (!thisTimeTable.Any())
                {
                    newItem.Add($"{weekdays[i]} {today}", new[] {"Geen Les", "Geen Les" });
                }
                else if (thisTimeTable.Count() == 1)
                {
                    if(thisTimeTable.First().Time.StartTime.Hour < 13 && thisTimeTable.First().Time.StartTime.Hour > 7)
                        newItem.Add($"{weekdays[i]} {today}", new[] { $"{thisTimeTable.First().Subject}", "Geen Les" });
                    if(thisTimeTable.First().Time.StartTime.Hour > 12 && thisTimeTable.First().Time.StartTime.Hour < 18)
                        newItem.Add($"{weekdays[i]} {today}", new[] { "Geen Les", $"{thisTimeTable.First().Subject}"});
                }
                else
                {
                    newItem.Add($"{weekdays[i]} {today}", new[]
                    {
                        $"{thisTimeTable.First().Subject}", 
                        $"{thisTimeTable.Last().Subject}"
                    });
                }
                
            }
            list.Add(newItem);
            string ret = formatter.FormatDictionaries(list);

            await Context.Channel.SendMessageAsync("```sql\r\n" + ret + "``` **VM: 08:30 - 12:00, NM: 13:00 - 16:30**");
        }
    }
}

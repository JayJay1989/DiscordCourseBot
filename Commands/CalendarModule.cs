using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
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
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            Dictionary<string, string> newItem = new Dictionary<string, string>();
            for (int i = 1; i <= 5; i++)
            {
                IEnumerable<TimeTable> thisTimeTable = timeTable.Where(tt => (int) tt.Day.DayOfWeek == i);
                if (!thisTimeTable.Any())
                {
                    newItem.Add(weekdays[i], "Geen Les");
                }
                else if (thisTimeTable.Count() == 1)
                {
                    newItem.Add(weekdays[i], $"{thisTimeTable.First().Subject} ({thisTimeTable.First().Time.StartTime:HH:mm}-{thisTimeTable.First().Time.EndTime:HH:mm})");
                }
                else
                {
                    newItem.Add(weekdays[i], $"{thisTimeTable.First().Subject} ({thisTimeTable.First().Time.StartTime:HH:mm}-{thisTimeTable.First().Time.EndTime:HH:mm}) | " +
                                             $"{thisTimeTable.Last().Subject} ({thisTimeTable.Last().Time.StartTime:HH:mm}-{thisTimeTable.Last().Time.EndTime:HH:mm})");
                }
                
            }
            list.Add(newItem);
            string ret = formatter.FormatDictionaries(list);

            await Context.Channel.SendMessageAsync("```sql\r\n" + ret + "```");
        }
    }
}

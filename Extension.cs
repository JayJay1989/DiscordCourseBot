using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ical.Net;

namespace DiscordBot
{
    public static class Extension
    {
        public static async Task<Calendar> LoadFromUriAsync(this Calendar calendar, Uri uri)
        {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(uri))
            {
                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadAsStringAsync();
                    return Calendar.Load(result);
            }
        }

        public static Calendar LoadFromUri(this Calendar calendar, Uri uri)
        {
            using (var client = new HttpClient())
            using (var response = client.GetAsync(uri).Result)
            {
                response.EnsureSuccessStatusCode();
                var result = response.Content.ReadAsStringAsync().Result;
                return Calendar.Load(result);
            }
        }

        public static string ShowAll(this List<Tasks> taskList)
        {
            StringBuilder stringBuilder = new StringBuilder("Tasks:\n\r");
            foreach (Tasks task in taskList)
            {
                stringBuilder.Append($"[{task.Course}] {task.Title} tenlaatste {task.EndTime:dd/MM/yyy HH:mm}\r\n");
            }

            if (!taskList.Any()) stringBuilder.Append("No tasks");
            return stringBuilder.ToString();
        }
    }
}

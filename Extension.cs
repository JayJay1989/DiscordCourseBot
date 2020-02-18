using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Models;
using Ical.Net;

namespace DiscordBot
{
    public static class Extension
    {
        /// <summary>
        /// Async method to load ical calendar from feed
        /// </summary>
        /// <param name="calendar"></param>
        /// <param name="uri">the url</param>
        /// <returns><seealso cref="Calendar"/>Calendar</returns>
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

        /// <summary>
        /// method to load ical calendar from feed
        /// </summary>
        /// <param name="calendar"></param>
        /// <param name="uri">the url</param>
        /// <returns><seealso cref="Calendar"/>Calendar</returns>
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

        /// <summary>
        /// Method to create a list of tasks
        /// </summary>
        /// <param name="taskList"></param>
        /// <returns></returns>
        public static string ShowAll(this List<Tasks> taskList)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Tasks task in taskList)
            {
                stringBuilder.Append($"- [{task.Course}] {task.Title} tenlaatste {task.EndTime:dd/MM/yyy HH:mm}\r\n");
            }

            if (!taskList.Any()) stringBuilder.Append("No tasks");
            return stringBuilder.ToString();
        }
    }
}

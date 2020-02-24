using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Models;
using Ical.Net;
using Microsoft.Extensions.Configuration;

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
        public static async Task<Calendar>? LoadFromUriAsync(this Calendar calendar, Uri uri)
        {
            Calendar ret = null;
            try
            {
                using (var client = new HttpClient())
                    using (var response = await client.GetAsync(uri))
                    {
                            response.EnsureSuccessStatusCode();
                            var result = await response.Content.ReadAsStringAsync();
                            ret = Calendar.Load(result);
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return ret;
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
                stringBuilder.Append($"+ [{task.Course}] {task.Title} tenlaatste {task.EndTime:dd/MM/yyy HH:mm}\r\n");
            }

            if (!taskList.Any()) stringBuilder.Append("No tasks");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Method to create a list of tasks
        /// </summary>
        /// <param name="taskList"></param>
        /// <returns></returns>
        public static string ShowAllPE(this List<Tasks> taskList)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (Tasks task in taskList)
            {
                stringBuilder.Append($"- [{task.Course}] {task.Title} tenlaatste {task.EndTime:dd/MM/yyy HH:mm}\r\n");
            }

            if (!taskList.Any()) stringBuilder.Append("No tasks");
            return stringBuilder.ToString();
        }

        public static Tasks GetLatestTask(this List<Tasks> newList, List<Tasks> oldList)
        {
            return newList.First(tasks => !oldList.Contains(tasks));
        }

        public static List<Tasks> ApplyBlacklist(this List<Tasks> taskList)
        {
            List<Tasks> retList = new List<Tasks>();
            string[] excludeList = Program.ThisProgram.GetConfig().GetSection("exclude").AsEnumerable().Where(p => p.Value != null).Select(p => p.Value).ToArray();
            foreach (Tasks task in taskList)
            {
                bool isValid = excludeList.All(bl => !task.Course.ToLower().Contains(bl));
                if (isValid)
                {
                    retList.Add(task);
                }
            }

            return retList;
        }

        /// <summary>
        /// Discord AddField message cannot be longer than 1024 characters
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string LimitMessage(this string message)
        {
            if (message.Length > 1000)
                return message.Substring(0, 1000)+"...";
            return message;
        }
    }
}

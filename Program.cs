using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Models;
using DiscordBot.Models.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot
{
    /// <summary>
    /// Program
    /// </summary>
    class Program
    {
        private DiscordSocketClient _client;
        private List<Message> _messages;
        private List<TimeTable> _timeTables;
        private List<Tasks> _taskList;
        private IConfiguration _config;
        static Timer _timer;
        public static Program ThisProgram;

        /// <summary>
        /// Constructor
        /// </summary>
        public Program()
        {
            ThisProgram=this;
            _messages = new List<Message>();
            var _builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile(path: "config.json");
            _config = _builder.Build();

            _taskList = new ICSDownloader().GetTaskList();
            _timeTables = new ICSConverter().GetTables();

            _timer = new Timer {AutoReset = false, Interval = GetInterval(int.Parse(_config["minutesToRefresh"])) };
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        /// <summary>
        /// Timer Elapsed
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">Arguments<see cref="ElapsedEventArgs"/></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(_messages.Count > 0)
                foreach (Message message in _messages)
                {
                    Task.Run(() => EditMessageTask(message.ThisMessage));
                }
            _taskList = new ICSDownloader().GetTaskList();
            int count = GetTasksSevenDays().Count;
            if (count > 0) SetBotTitle($"{count} {(count > 1 ? "taken" : "taak")} deze week");
            _timer.Interval = GetInterval(int.Parse(_config["minutesToRefresh"]));
            _timer.Start();
        }

        /// <summary>
        /// Edit Message
        /// </summary>
        /// <param name="messageParam">The original message  <see cref="RestUserMessage"/></param>
        /// <returns></returns>
        private async Task EditMessageTask(RestUserMessage messageParam)
        {
            if (messageParam == null) return;
            await messageParam.ModifyAsync(msg => msg.Embed = Calculate());
        }

        /// <summary>
        /// Calculate if there is a Course
        /// </summary>
        /// <returns>Discord Embed</returns>
        private Embed Calculate()
        {
            DateTime now = DateTime.Now;
            IEnumerable<TimeTable> timeTable = _timeTables.Where((tt) => tt.Day == DateTime.Today);
            return getCurrenTimeTable(timeTable) == null ? CreateBlankEmbed() : CreateEmbed(getCurrenTimeTable(timeTable));
        }

        /// <summary>
        /// Get current timetable.
        /// </summary>
        /// <param name="timeTables">All timetable items</param>
        /// <returns>The timetable</returns>
        private TimeTable? getCurrenTimeTable(IEnumerable<TimeTable> timeTables)
        {

            TimeTable ret = null;
            try
            {
                ret = timeTables.Single(tt =>
                    tt.Time.StartTime.Hour >= DateTime.Now.Hour && tt.Time.EndTime.Hour <= DateTime.Now.Hour);
            }
            catch (Exception e)
            {
            }
            return ret;
        }

        /// <summary>
        /// The interval
        /// </summary>
        /// <returns></returns>
        static double GetInterval(int interval)
        {
            DateTime now = DateTime.Now;
            return ((60 - now.Second) * (1000 * interval) - now.Millisecond); //every minute
        }

        /// <summary>
        /// Message Received
        /// </summary>
        /// <param name="messageParam">Received message<see cref="SocketMessage"/></param>
        /// <returns>-</returns>
        private async Task MessageReceivedAsync(SocketMessage messageParam)
        {
            var rMessage = (RestUserMessage)await messageParam.Channel.GetMessageAsync(messageParam.Id);
            
            if (rMessage.Content == "!monitor" && !rMessage.Author.IsBot && rMessage.Author.Username.Contains("JayJay1989BE"))
            {
                await rMessage.DeleteAsync();
                var test = await messageParam.Channel.SendMessageAsync(embed:Calculate());
                if (!DoesItExist(test))
                {
                    await AddMessage(test);
                }
            }

            if (rMessage.Content == "!lesrooster")
            {
                await rMessage.DeleteAsync();
                await messageParam.Channel.SendMessageAsync(embed: Calculate());
            }
        }

        /// <summary>
        /// Create embed with provided Timetable data
        /// </summary>
        /// <param name="timeTable">The timetable</param>
        /// <returns>Embed</returns>
        private Embed CreateEmbed(TimeTable timeTable)
        {
            List<Tasks> tasklist = GetTasksSevenDays();
            return new EmbedBuilder()
                .AddField("Teacher", timeTable.Teacher)
                .AddField("Class Room", timeTable.ClassRoom)
                .AddField("Start", $"{timeTable.Time.StartTime.Hour}:{timeTable.Time.StartTime.Minute:00}", true)
                .AddField("End", $"{timeTable.Time.EndTime.Hour}:{timeTable.Time.EndTime.Minute::00}", true)
                .AddField("Tasks:", $"```\n{tasklist.ShowAll()}\n```")
                .WithColor(Color.Green)
                .WithTitle($"Nu: {timeTable.Subject}")
                .WithFooter($"Last updated on: {DateTime.Now}")
                .Build();
        }

        /// <summary>
        /// Create Blank embed when there is no course. Show next course
        /// </summary>
        /// <returns>Embed</returns>
        private Embed CreateBlankEmbed()
        {
            TimeTable nextCourse = NextCourse();
            List<Tasks> tasklist = GetTasksSevenDays();
            return new EmbedBuilder()
                .AddField("Next Course", nextCourse.Subject)
                .AddField("Class room", nextCourse.ClassRoom)
                .AddField("Date", $"{nextCourse.Day:dd/MM/yyyy}")
                .AddField("Start", $"{nextCourse.Time.StartTime.Hour}:{nextCourse.Time.StartTime.Minute:00}", true)
                .AddField("End", $"{nextCourse.Time.EndTime.Hour}:{nextCourse.Time.EndTime.Minute:00}", true)
                .AddField("Tasks:", $"```\n{tasklist.ShowAll()}\n```")
                .WithColor(Color.Red)
                .WithTitle("Nu: Geen Les")
                .WithFooter($"Last updated on: {DateTime.Now}")
                .Build();
        }

        /// <summary>
        /// Next Course
        /// </summary>
        /// <returns></returns>
        private TimeTable NextCourse()
        {
            return _timeTables.First(tt => tt.Time.StartTime > DateTime.Now);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="weeknumber"></param>
        /// <returns></returns>
        public IEnumerable<TimeTable> GetWeekTable(string weeknumber = null)
        {
            int WeekNumber = (weeknumber == null) ? GetWeekNumber(DateTime.Now) : int.Parse(weeknumber);
            List<TimeTable> ret = new List<TimeTable>();
            foreach (TimeTable timeTable in _timeTables)
            {
                if(GetWeekNumber(timeTable.Day.Date).Equals(WeekNumber)) ret.Add(timeTable);
            }
            return ret;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public int GetWeekNumber(DateTime date)
        {
            CultureInfo ciCurr = CultureInfo.CurrentCulture;
            int weekNum = ciCurr.Calendar.GetWeekOfYear(date,
                CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return weekNum;
        }

        private List<Tasks> GetTasksSevenDays()
        {
            List<Tasks> ret = new List<Tasks>();
            ret = _taskList.FindAll(t => t.EndTime <= DateTime.Today.AddDays(7) && t.EndTime >= DateTime.Today);
            return ret;
        }

        /// <summary>
        /// Ready
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");
            _client.SetActivityAsync(new Game("Lessenrooster", ActivityType.Watching));
            return Task.CompletedTask;
        }

        private void SetBotTitle(string title)
        {
            _client.SetActivityAsync(new Game(title, ActivityType.Watching));
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="log">the log</param>
        /// <returns><see cref="Task"/></returns>
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Main Task
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        public async Task MainAsync()
        {
            
            using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();
                _client = client;
                client.Log += LogAsync;
                client.Ready += ReadyAsync;
                client.MessageReceived += MessageReceivedAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;
                await client.LoginAsync(TokenType.Bot, _config["Token"]);
                await client.StartAsync();
                await services.GetRequiredService<CommandHandler>().InitializeAsync();
                await Task.Delay(-1);
            }
            string cki;
            while (true)
            {
                cki = Console.ReadLine();
                if (cki.ToLower().Equals("stop"))
                {
                    await _client.StopAsync();
                    break;
                }
            }
        }

        private ServiceProvider ConfigureServices()
        {
            // this returns a ServiceProvider that is used later to call for those services
            // we can add types we have access to here, hence adding the new using statement:
            // using csharpi.Services;
            // the config we build is also added, which comes in handy for setting the command prefix!
            return new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();
        }

        /// <summary>
        /// Add message
        /// </summary>
        /// <param name="message">The message <see cref="RestUserMessage"/></param>
        /// <returns></returns>
        private async Task AddMessage(RestUserMessage message)
        {
            if (DoesItExist(message))
            {
                return;
            }
            _messages.Add(new Message(message));
        }


        /// <summary>
        /// Does it exist
        /// </summary>
        /// <param name="message">the message <see cref="RestUserMessage"/></param>
        /// <returns>the result</returns>
        private bool DoesItExist(RestUserMessage message)
        {
            return _messages.Exists(message1 => message1.ThisMessage == message);
        }

        public IConfiguration GetConfig()
        {
            return _config;
        }
    }
}
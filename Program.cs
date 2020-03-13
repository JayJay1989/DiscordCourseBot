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
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace DiscordBot
{
    /// <summary>
    /// Program
    /// </summary>
    class Program
    {
        private DiscordSocketClient _client;
        private List<Message> _messages;
        private List<Message> _markedMessages;
        private List<TimeTable> _timeTables;
        private List<Tasks> _taskList;
        private List<Tasks> _tasklistOld;
        private IConfiguration _config;
        static Timer _timer;
        public ILogger Logger;
        public static Program ThisProgram;
        private int taskCount = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Program()
        {
            Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File("log.txt")
                .CreateLogger();
            ThisProgram = this;
            _messages = _markedMessages = new List<Message>();
            var _builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile(path: "config.json");
            _config = _builder.Build();

            List<Tasks> resultCalendar = new ICSDownloader().GetTaskList().ApplyBlacklist();
            if (resultCalendar == null)
            {
                Logger.Information($"Failed to get calendar from url. got: {resultCalendar}");
                return;
            }

            _taskList = resultCalendar;
            _timeTables = new ICSConverter().GetTables();

            _timer = new Timer { AutoReset = false, Interval = GetInterval(int.Parse(_config["minutesToRefresh"])) };
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        /// <summary>
        /// Timer Elapsed
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">Arguments<see cref="ElapsedEventArgs"/></param>
        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            if (_messages.Count > 0)
            {
                foreach (Message message in _messages)
                {
                    await EditMessage(message.ThisMessage);
                }
            }

            if (_markedMessages.Count > 0)
            {
                foreach (Message markedMessage in _markedMessages)
                {
                    await RemoveMessage(markedMessage.ThisMessage);
                }
            }

            var calendarResult = new ICSDownloader().GetTaskList();
            if (calendarResult == null)
            {
                Logger.Information("CalendarResult was null, return");
                return;
            }
            _taskList = calendarResult.ApplyBlacklist();
            int countTasks = GetOnlyNewPETasks().Count;
            if (taskCount == 0)
            {
                taskCount = countTasks;
                _tasklistOld = _taskList;
            }
            else if (countTasks > taskCount)
            {
                await SendMessageToChannelAsync(_taskList.GetLatestTask(_tasklistOld));
                taskCount = countTasks;
                _tasklistOld = _taskList;
            }

            int count = GetTasksSevenDays().Count;
            if (count > 0) SetBotTitle($"{count} {(count > 1 ? "taken" : "taak")} deze week");
            _timer.Interval = GetInterval(int.Parse(_config["minutesToRefresh"]));
            _timer.Start();
            TimeSpan timeSpan = DateTime.Now - now;
            Logger.Information($"Timer_Elapsed: {timeSpan.Seconds} seconds");
        }

        private async Task<RestUserMessage> SendMessageToChannelAsync(Tasks task)
        {
            // 678954369764425728 => #les
            var channel = _client.GetChannel(678954369764425728) as SocketTextChannel;
            return await channel.SendMessageAsync($"@here **Nieuwe taak!!** [{task.Course}] {task.Title} indienen op {task.EndTime:dd-MM-yyyy HH:mm}");
        }

        /// <summary>
        /// Edit Message
        /// </summary>
        /// <param name="messageParam">The original message  <see cref="RestUserMessage"/></param>
        /// <returns></returns>
        private async Task EditMessage(RestUserMessage messageParam)
        {
            if (messageParam == null) return;
            try
            {
                await messageParam.ModifyAsync(msg => msg.Embed = Calculate());
            }
            catch (Exception e)
            {
                Logger.Information($"Error while editing message: {e.Message}");
            }
        }

        private async Task RemoveMessage(RestUserMessage messageParam)
        {
            if (messageParam == null) return;
            try
            {
                await messageParam.DeleteAsync();
            }
            catch (Exception e)
            {
                Logger.Information($"Error while removing message: {e.Message}");
            }
        }

        /// <summary>
        /// Calculate if there is a Course
        /// </summary>
        /// <returns>Discord Embed</returns>
        private Embed Calculate()
        {
            DateTime now = DateTime.Now;
            IEnumerable<TimeTable> timeTable = _timeTables.FindAll((tt) => tt.Day == DateTime.Today);
            return getCurrenTimeTable(timeTable) == null ? CreateBlankEmbed() : CreateEmbed(getCurrenTimeTable(timeTable));
        }

        /// <summary>
        /// Get current timetable.
        /// </summary>
        /// <param name="timeTables">All timetable items</param>
        /// <returns>The timetable</returns>
        private TimeTable? getCurrenTimeTable(IEnumerable<TimeTable> timeTables)
        {

            return timeTables.FirstOrDefault(tt => DateTime.Now >= tt.Time.StartTime & DateTime.Now <= tt.Time.EndTime); ;
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
                var sentMessage = await messageParam.Channel.SendMessageAsync(embed: Calculate());
                if (!DoesItExist(sentMessage))
                {
                    await AddMessage(sentMessage);
                }
            }

            if (rMessage.Content == "!lesrooster")
            {
                //await rMessage.DeleteAsync();
                await messageParam.Channel.SendMessageAsync(embed: Calculate());
            }

            if (rMessage.Content == "!tasks")
            {
                await messageParam.Channel.SendMessageAsync("\nPE tasks:```diff\n" + GetOnlyNewPETasks().ShowAllPE() + "\n```\n" +
                                                            "Reg. tasks:```diff\n" + GetOnlyNewTasks().ShowAll() + "```");
            }

            // Remove message that contains an image or other attachment
            if (rMessage.Attachments.Count > 0) await AddMarkedMessage(rMessage);
        }

        /// <summary>
        /// Create embed with provided Timetable data
        /// </summary>
        /// <param name="timeTable">The timetable</param>
        /// <returns>Embed</returns>
        private Embed CreateEmbed(TimeTable timeTable)
        {
            return new EmbedBuilder()
                .AddField("Teacher", timeTable.Teacher)
                .AddField("Class Room", timeTable.ClassRoom)
                .AddField("Start", $"{timeTable.Time.StartTime:HH:mm}", true)
                .AddField("End", $"{timeTable.Time.EndTime:HH:mm}", true)
                .AddField("Tasks PE:", $"```diff\n{GetOnlyNewPETasks().ShowAllPE().LimitMessage()}\n```")
                .AddField("Reg. Tasks:", $"```diff\n{GetOnlyNewTasks().ShowAll().LimitMessage()}\n```")
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
            return new EmbedBuilder()
                .AddField("Next Course", nextCourse.Subject)
                .AddField("Class room", nextCourse.ClassRoom)
                .AddField("Date", $"{nextCourse.Day:dd/MM/yyyy}")
                .AddField("Start", $"{nextCourse.Time.StartTime:HH:mm}", true)
                .AddField("End", $"{nextCourse.Time.EndTime:HH:mm}", true)
                .AddField("Tasks PE:", $"```diff\n{GetOnlyNewPETasks().ShowAllPE().LimitMessage()}\n```")
                .AddField("Reg. Tasks:", $"```diff\n{GetOnlyNewTasks().ShowAll().LimitMessage()}\n```")
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
            return _timeTables.FirstOrDefault(tt => tt.Time.StartTime > DateTime.Now);
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
                if (GetWeekNumber(timeTable.Day.Date).Equals(WeekNumber)) ret.Add(timeTable);
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
        /// 
        /// </summary>
        /// <returns></returns>
        private List<Tasks> GetOnlyNewTasks()
        {
            List<Tasks> result = new List<Tasks>();
            result = _taskList.FindAll(t => t.EndTime >= DateTime.Today && !t.Title.Contains("PE"));
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private List<Tasks> GetOnlyNewPETasks()
        {
            List<Tasks> result = new List<Tasks>();
            result = _taskList.FindAll(t => t.EndTime >= DateTime.Today && t.Title.Contains("PE"));
            return result;
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

        private async Task AddMarkedMessage(RestUserMessage markedMessage)
        {
            _markedMessages.Add(new Message(markedMessage));
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
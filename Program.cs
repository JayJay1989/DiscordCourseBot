using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Messages;
using Newtonsoft.Json;

namespace DiscordBot
{
    /// <summary>
    /// Program
    /// </summary>
    class Program
    {
        private readonly DiscordSocketClient _client;
        private List<Message> _messages;
        private List<Message> _messagesToRemove;
        private List<TimeTable> _timeTables;
        private Config _cnf;
        static Timer _timer;

        /// <summary>
        /// Constructor
        /// </summary>
        public Program()
        {

            _messages = new List<Message>();
            _messagesToRemove = new List<Message>();
            _timeTables = new List<TimeTable>();

            _timeTables = JsonConvert.DeserializeObject<List<TimeTable>>(
                    File.ReadAllText($"{Environment.CurrentDirectory}\\timetable.json"));

            _cnf = JsonConvert.DeserializeObject<Config>(
                    File.ReadAllText($"{Environment.CurrentDirectory}\\config.json"));

            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _timer = new Timer {AutoReset = false, Interval = GetInterval()};
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        /// <summary>
        /// Console On Cancel Key Press Task (CTRL+C)
        /// </summary>
        /// <returns>-</returns>
        private async Task ConsoleOnCancelKeyPress()
        {
            _timer.Stop();
            foreach (Message message in _messages)
            {
                await Task.Run(() => DeletedMessageTask(message.ThisMessage));
            }
            foreach (Message message in _messagesToRemove)
            {
                await Task.Run(() => DeletedMessageTask(message.ThisMessage));
            }
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
            _timer.Interval = GetInterval();
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
        /// Delete Message
        /// </summary>
        /// <param name="messageParam">The original message <see cref="RestUserMessage"/></param>
        /// <returns></returns>
        private async Task DeletedMessageTask(RestUserMessage messageParam)
        {
            if (messageParam == null) return;
            await messageParam.DeleteAsync();
        }

        /// <summary>
        /// The interval
        /// </summary>
        /// <returns></returns>
        static double GetInterval()
        {
            DateTime now = DateTime.Now;
            return ((60 - now.Second) * 1000 - now.Millisecond); //every minute
        }

        /// <summary>
        /// Message Received
        /// </summary>
        /// <param name="messageParam">Received message<see cref="SocketMessage"/></param>
        /// <returns>-</returns>
        private async Task MessageReceivedAsync(SocketMessage messageParam)
        {
            var rMessage = (RestUserMessage)await messageParam.Channel.GetMessageAsync(messageParam.Id);
            _messagesToRemove.Add(new Message(rMessage));
            if (rMessage.Content == "!monitor" && !rMessage.Author.IsBot)
            {
                var test = await messageParam.Channel.SendMessageAsync(embed:Calculate());
                if (!DoesItExist(test))
                {
                    await AddMessage(test);
                }
            }
        }

        /// <summary>
        /// Create embed with provided Timetable data
        /// </summary>
        /// <param name="timeTable">The timetable</param>
        /// <returns>Embed</returns>
        private Embed CreateEmbed(TimeTable timeTable)
        {
                return new EmbedBuilder()
                .AddField("Teacher", timeTable.Teacher, true)
                .AddField("Class Room", timeTable.ClassRoom, true)
                .AddField("","", true)
                .AddField("Start", $"{timeTable.Time.StartTime.Hour}:{timeTable.Time.StartTime.Minute:00}", true)
                .AddField("End", $"{timeTable.Time.EndTime.Hour}:{timeTable.Time.EndTime.Minute::00}", true)
                .AddField("","", true)
                .WithColor(Color.Green)
                .WithTitle(timeTable.Subject)
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
                .AddField("Next Course", nextCourse.Subject, true)
                .AddField("Class room", nextCourse.ClassRoom, true)
                .AddField("","", true)
                .AddField("Date", $"{nextCourse.Day:dd/MM/yyyy}", true)
                .AddField("Start", $"{nextCourse.Time.StartTime.Hour}:{nextCourse.Time.StartTime.Minute:00}", true)
                .AddField("End", $"{nextCourse.Time.EndTime.Hour}:{nextCourse.Time.EndTime.Minute:00}", true)
                .WithColor(Color.Red)
                .WithTitle("Geen Les")
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
        /// Ready
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");
            _client.SetActivityAsync(new Game("Lessenrooster", ActivityType.Watching));
            return Task.CompletedTask;
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
            ConsoleKeyInfo cki;
            await _client.LoginAsync(TokenType.Bot, _cnf.bot.token);
            await _client.StartAsync();
            while (true)
            {
                cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.X)
                {
                    await ConsoleOnCancelKeyPress();
                    await _client.StopAsync();
                    break;
                }
            }
            await Task.Delay(-1);
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
    }
}

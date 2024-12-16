using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using ShrekBot.Modules.Configuration;
using ShrekBot.Modules.Data_Files_and_Management;
using Interactivity;
using ShrekBot.Modules.Swamp.Services;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ShrekBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private EventCooldownManager _eventCooldown;
        private readonly ConcurrentDictionary<ulong, DateTimeOffset> _eventExecutionTimestamps
            = new ConcurrentDictionary<ulong, DateTimeOffset>();
        // /*public static*/ private LavaNode _lavaNode;

        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            { //perhaps it's an intents issue to why the bot can't join vc
                LogLevel = LogSeverity.Verbose,
                GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.DirectMessages | GatewayIntents.GuildMessageTyping |
                GatewayIntents.GuildMessages | GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates | GatewayIntents.GuildMessages,
                AlwaysDownloadUsers = true
            });
            _commands = new CommandService();
            _eventCooldown = new EventCooldownManager();
            //_lavaNode = new LavaNode(_client, new LavaConfig());

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                //.AddLavaNode(x =>
                //{
                //    //created lambha expression in case I want to do something here
                //    x.SelfDeaf = true;
                //})
                .AddSingleton<AudioService>()
                .AddSingleton<TimerService>()
                .AddSingleton<InteractivityService>()
                .AddSingleton(new InteractivityConfig 
                {
                    DefaultTimeout = TimeSpan.FromSeconds(30) // You can optionally add a custom config
                }) 
            .BuildServiceProvider();
            //ideally want to hide the fact this bot is online from Drake...
            //but it's not working
            //await _client.SetStatusAsync(UserStatus.Offline);

            _client.Log += _client_Log;
            _client.UserJoined += _client_UserJoined;
            _client.MessageReceived += _client_MessageRecieved;
            //_client.Ready += _client_OnReady;

            await RegisterCommandsAsync();
            await _client.LoginAsync(TokenType.Bot, Config.bot.Token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        //private async Task _client_OnReady()
        //{
        //    if (!_lavaNode.IsConnected)
        //    {
        //        try
        //        {
        //            await _lavaNode.ConnectAsync();
        //        }
        //        catch(Exception ex)
        //        {
        //            Console.WriteLine(ex.Message);
        //        }
        //    }
        //}

        private async Task _client_UserJoined(SocketGuildUser user)
        {     
            if (user.IsBot || user.IsWebhook)
                return;
            const ulong DrakeServer = 508834039491330049;
            if (user.Guild == _client.GetGuild(DrakeServer)) //if the person joined Drake's discord...
            {
                const ulong WelcomeChannel = 553092290990571535;
                IMessageChannel chnl = _client.GetChannel(WelcomeChannel) as IMessageChannel;
                if (chnl != null)
                {
                    ShrekMessage swamp = new ShrekMessage(true);

                    await chnl.SendMessageAsync($"{user.Mention}!?!? " +
                        $"{Environment.NewLine}{Environment.NewLine} {swamp.GetValue("1")}");
                }

            }
        }

        private int Search(string pattern, string text)
        {
            return text.IndexOf(pattern, 0, StringComparison.CurrentCultureIgnoreCase);
        }

        private async Task TextRecieved(SocketMessage socketMessage)
        {
            bool onCooldown = _eventCooldown.IsMessageOnCooldown(socketMessage.Author.Id);
            if (onCooldown)
                return;

            ShrekGIFs gifs = new ShrekGIFs();
            for (int i = 0; i < gifs.SearchKeys.Length; i++)
            {
                string key = gifs.SearchKeys[i];
                int value = Search(key, socketMessage.Content);
                if (value > -1)
                    await socketMessage.Channel.SendMessageAsync(gifs.GetValue(key));
            }
        }

        private async Task PictureRecieved(SocketMessage socketMessage)
        {
            bool onCooldown = _eventCooldown.IsImageOnCooldown(socketMessage.Author.Id);
            if (onCooldown)
                return;
            if (socketMessage.Attachments.Count > 0)
            {
                IEnumerable<Discord.Attachment> image = socketMessage.Attachments.Where(x =>
                    x.Filename.EndsWith(".jpg") ||
                    x.Filename.EndsWith(".png"));

                //time to do the image detection algorithm thingy...

                if(image.Any())
                    await socketMessage.Channel.SendMessageAsync("Image checks out");
            }
        }
        private async Task _client_MessageRecieved(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot)
                return;
       
            if (socketMessage.Channel.Id == 653106031731408896)//can send in one discord channel only
            {
                //if the user is not using a command
                if (socketMessage.Content[0] != Convert.ToChar(Config.bot.Prefix))
                {                   
                    await TextRecieved(socketMessage);
                }
                //if(!imageCheckedOrSent)        
                //    await PictureRecieved(socketMessage);               
            }

            //if the user only sent an image
            if (string.IsNullOrEmpty(socketMessage.Content))
            {
                await PictureRecieved(socketMessage);
            }
        }

        private Task _client_Log(LogMessage arg)
        {
            //_lavaNode.OnLog += arg;
            Console.WriteLine(arg.Message /*+ " " + DateTime.Now*/);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            if (message.Author.IsBot)
                return;

            int argPos = 0;
            if (message.HasStringPrefix(Config.bot.Prefix, ref argPos))
            {
                var context = new SocketCommandContext(_client, message);
                var result = await _commands.ExecuteAsync(context, argPos, _services);
                if (!result.IsSuccess)
                    Console.WriteLine(result.ErrorReason, DateTime.Now);
            }
        }
    }
}

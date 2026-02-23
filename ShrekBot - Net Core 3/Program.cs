using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Shrekbot;
using ShrekBot.Modules.Configuration;
using ShrekBot.Modules.Data_Files_and_Management;
using ShrekBot.Modules.Database;
using ShrekBot.Modules.Swamp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShrekBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        private EventCooldownManager _eventCooldown;
        private ExtractWebLinkInfo _webLinkInfo;
        private WhiteListedChannels _textChannelWhiteList;

        private SwampDB _database;
        
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
            _webLinkInfo = new ExtractWebLinkInfo();
            _textChannelWhiteList = new WhiteListedChannels();
            _database = new SwampDB();
            //_lavaNode = new LavaNode(_client, new LavaConfig());
            //
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
            _client.UserLeft += _client_UserLeft;
            _client.UserBanned += _client_UserBanned;
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
                    _database.AddNewFriend(user.Id, user.Username);
                }

            }
        }
        private async Task _client_UserLeft(SocketGuild guild, SocketUser user)
        {
            if (user.IsBot || user.IsWebhook)
                return;

            _database.RemoveFriend(user.Id);
            await Task.CompletedTask;
        }

        private async Task _client_UserBanned(SocketUser user, SocketGuild guild)
        {
            if (user.IsBot || user.IsWebhook)
                return;

            _database.RemoveFriend(user.Id);
            await Task.CompletedTask;
        }

        private enum UserMessageStatus
        {
            OnCooldown,
            DoNotDelete,
            Delete
        }

        private static int Search(string pattern, string text)
        {
            return text.IndexOf(pattern, 0, StringComparison.CurrentCultureIgnoreCase);
        }

        //https://stackoverflow.com/a/16665247/9521550
        private async Task<UserMessageStatus> TextRecieved(SocketMessage socketMessage)
        {
            SocketGuildChannel messageChannel = (SocketGuildChannel)socketMessage.Channel;
            ulong userid = socketMessage.Author.Id;
            //await socketMessage.Channel.SendMessageAsync($" {socketMessage.Timestamp}");
            //check if user sent a link, do it before the cooldown command to circumvent it
            UrlDetails postedLink = _webLinkInfo.ExtractURL(socketMessage.Content, 
                new Tuple<ulong, ulong, ulong>
                    (messageChannel.Guild.Id, socketMessage.Channel.Id, socketMessage.Id));
            if (!_webLinkInfo.IsUrlDetailsEmpty(postedLink))
            {
                //Get
                UrlDetails[] extractedLinksFromSameUser = _database.SelectFrom_Table(_webLinkInfo.Domain, userid, postedLink.UrlId);
                string[] extractedMessageLinksFromDifferentUsers = _database.SelectFrom_Table(_webLinkInfo.Domain, postedLink.UrlId);
                //string[] combined = extractedLinksFromSameUser.Union<string>(extractedMessageLinksFromDifferentUsers).ToArray();

                //Insert after, so that we don't trigger the DDD meme on the first unique link
                int records = _database.InsertInto_Table(_webLinkInfo.Domain, postedLink, userid);
                if (extractedLinksFromSameUser.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach(UrlDetails extracted in extractedLinksFromSameUser)
                    {
                        sb.AppendLine(_webLinkInfo.CreateDiscordTextChannelURL(extracted));
                    }
                    ReplyDDDMeme(socketMessage, sb.ToString());
                }
                
                //await socketMessage.Channel.SendMessageAsync($"{_webLinkInfo.CreateDiscordTextChannelURL(link)}"); //{link.ToString()} + {socketMessage.Timestamp}
            }

            bool onCooldown = _eventCooldown.IsMessageOnCooldown(userid);
            if (onCooldown)
                return UserMessageStatus.OnCooldown;

            ShrekGIFs gifs = new ShrekGIFs();
            for (int i = 0; i < gifs.SearchKeys.Length; i++)
            {
                string key = gifs.SearchKeys[i];
                int value = Search(key, socketMessage.Content);
                if (value > -1)
                    await socketMessage.Channel.SendMessageAsync(gifs.GetValue(key));
            }
            return UserMessageStatus.DoNotDelete;
        }

        private async Task<UserMessageStatus> PictureRecieved(SocketMessage socketMessage)
        {
            bool onCooldown = _eventCooldown.IsImageOnCooldown(socketMessage.Author.Id);
            if (onCooldown)
                return UserMessageStatus.OnCooldown;
            if (socketMessage.Attachments.Count > 0)
            {
                //TODO: find out why it deletes image when there is only an image, if there is text, it won't be deleted
                //await socketMessage.DeleteAsync();

                IEnumerable<Attachment> attachments = socketMessage.Attachments;
                HttpClient _httpClient = new HttpClient();


                //IEnumerable<Discord.Attachment> image = socketMessage.Attachments.Where(x =>
                //    x.Filename.EndsWith(".jpg") ||
                //    x.Filename.EndsWith(".png"));
                bool doesDuplicateExist = false;
                foreach (Attachment media in attachments)
                {
                    //TODO: Implement a proper check for when attachments are images or videos
                    //time to do the image detection algorithm thingy...
                    await using (Stream stream = await _httpClient.GetStreamAsync(media.Url))
                    {
                        //TODO: Check if current Attachment is an image
                        var test = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(stream);
                        await socketMessage.Channel.SendMessageAsync("Imagesharp size should print... " + test.Size.ToString());
                        test.Dispose();
                    }

                    
                    //byte[] bytes = BitConverter.GetBytes(img.Size);
                    //using (MemoryStream ms = new MemoryStream(bytes))
                    //{

                    //    ms.Seek(0, SeekOrigin.Begin);
                    //    var test = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(ms);
                    //    await socketMessage.Channel.SendMessageAsync(test.Height.ToString());
                    //    using (FileStream fs = new FileStream(img.Filename, FileMode.Create, FileAccess.Write))
                    //    {
                    //        ms.CopyTo(fs);
                    //        fs.Position = 0;
                    //        await socketMessage.Channel.SendMessageAsync("Filestream Byte Length " + fs.Length.ToString());
                    //        var test = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(fs);

                    //        await socketMessage.Channel.SendMessageAsync("Imagesharp size should print... " + test.Size.ToString());

                    //    }
                    //}
                    

                    //var test = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(img.Url); //it "crashes" right here...
                    //await socketMessage.Channel.SendMessageAsync(test.Height.ToString());
                    //await socketMessage.Channel.SendMessageAsync(img.ProxyUrl);
                }
                //Parallel.ForEach(images, async img =>
                //{
                //    await socketMessage.Channel.SendMessageAsync(img.Size.ToString());
                //});

                if(doesDuplicateExist)
                {
                    StringBuilder message = new StringBuilder("");//reply to original poster, post the message link
                    using (FileStream fs = File.Open("ddd.jpg", FileMode.Open))
                        await socketMessage.Channel.SendFileAsync(fs, "ddd.jpg", message.ToString());
                }
            }
            return UserMessageStatus.DoNotDelete;
        }

        private async void ReplyDDDMeme(SocketMessage socketMessage, string message)
        {
            using (FileStream fs = File.Open("ddd.jpg", FileMode.Open))
                await socketMessage.Channel.SendFileAsync(fs, "ddd.jpg", message.ToString());
        }
        private async Task _client_MessageRecieved(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook)
                return;
            
            if (_textChannelWhiteList.ContainsId(socketMessage.Channel.Id))//(socketMessage.Channel.Id == 653106031731408896)
            {     
                if (!string.IsNullOrEmpty(socketMessage.Content))
                {
                    //if the user is not using a command
                    if (socketMessage.Content[0] != Convert.ToChar(Config.bot.Prefix))
                        await TextRecieved(socketMessage);
                }             
            }

            //Image detection is checked aganist every text channel the bot has access to because
            //Drake spams that abomination in every channel in the server

            //if the user only sent an image
            //if (socketMessage.Attachments.Count > 0)
            //{
            //    UserMessageStatus code = await PictureRecieved(socketMessage);
            //    if (code == UserMessageStatus.Delete)
            //        await socketMessage.DeleteAsync();
            //}

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

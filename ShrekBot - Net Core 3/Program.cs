using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Shrekbot;
using ShrekBot.Modules.Configuration;
using ShrekBot.Modules.Data_Files_and_Management;
using ShrekBot.Modules.Data_Files_and_Management.Database;
using ShrekBot.Modules.Swamp.Helpers;
using ShrekBot.Modules.Swamp.Services;
using ShrekBot.Modules.User_Functions;
using SixLabors.ImageSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShrekBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        private EventCooldownManager _eventCooldown;
        //private ExtractWebLinkInfo _webLinkInfo;
        //private TextChannelWhiteList _textChannelWhiteList;
        //internal ImageComparison _imagesAndVideoComparison;
    
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
            //_webLinkInfo = new ExtractWebLinkInfo();
            //_textChannelWhiteList = new TextChannelWhiteList();
            //_imagesAndVideoComparison = new ImageComparison();
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
            _client.UserLeft += _client_UserLeft;
            _client.UserBanned += _client_UserBanned;
            _client.ChannelDestroyed += _client_TextChannelDeleted;
            _client.MessageReceived += _client_MessageRecieved;
            _client.Ready += _client_OnReady;

            await RegisterCommandsAsync();
            await _client.LoginAsync(TokenType.Bot, Config.bot.Token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task _client_OnReady()
        {
            //if (!_lavaNode.IsConnected)
            //{
            //    try
            //    {
            //        await _lavaNode.ConnectAsync();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}
            _ = new SwampDB(); //for calling the static method that initializes the database
            await Task.CompletedTask;
        }

        private async Task _client_UserJoined(SocketGuildUser user)
        {     
            if (user.IsBot || user.IsWebhook)
                return;
            const ulong DrakeServer = 508834039491330049;
            if (user.Guild == _client.GetGuild(DrakeServer)) //if the person joined Drake's discord...
            {
                const ulong WelcomeChannel = 553092290990571535;
                ITextChannel chnl = _client.GetChannel(WelcomeChannel) as ITextChannel;
                if (chnl != null)
                {
                    ShrekMessage swamp = new ShrekMessage(true);

                    await chnl.SendMessageAsync($"{user.Mention}!?!? " +
                        $"{Environment.NewLine}{Environment.NewLine} {swamp.GetValue("1")}");
                    SwampDB.AddNewFriend(user.Id, user.Username);
                }
            }
        }
        private async Task _client_UserLeft(SocketGuild guild, SocketUser user)
        {
            if (user.IsBot || user.IsWebhook)
                return;

            SwampDB.RemoveFriend(user.Id);
            await Task.CompletedTask;
        }

        private async Task _client_UserBanned(SocketUser user, SocketGuild guild)
        {
            if (user.IsBot || user.IsWebhook)
                return;

            SwampDB.RemoveFriend(user.Id);
            await Task.CompletedTask;
        }

        private async Task _client_TextChannelDeleted(SocketChannel channel)
        {
            if(TextChannelWhiteList.ContainsId(channel.Id))
                TextChannelWhiteList.Remove(channel.Id);
            await Task.CompletedTask;
        }

        private enum UserMessageStatus
        {
            None,
            TripleD,
            OnCooldown,
            DoNotDelete,
            Delete
        }

        private struct DDDReply
        {
            public UserMessageStatus Code { get; set; }
            public string Message { get; set; }

            public DDDReply()
            {
                Code = UserMessageStatus.None;
                Message = "";
            }
        }
        private static int Search(string pattern, string text)
        {
            return text.IndexOf(pattern, 0, StringComparison.CurrentCultureIgnoreCase);
        }

        private ConcurrentBag<string> ParallelGIFSearch(string messageContent)
        {
            ConcurrentBag<string> values = new ConcurrentBag<string>();
            ShrekGIFs gifs = new ShrekGIFs();
            Parallel.ForEach(ShrekGIFs.SearchKeys, searchKey =>
            {
                int value = Search(searchKey, messageContent);
                if (value > -1)
                    values.Add(gifs.GetValue(searchKey));
            });

            return values;
        }

        //https://stackoverflow.com/a/16665247/9521550
        private async Task<DDDReply>TextRecieved(SocketMessage socketMessage)
        {
            SwampDB database = new SwampDB();
            ExtractWebLinkInfo webLinkInfo = new ExtractWebLinkInfo();
            DDDReply hollerNHoot = new DDDReply();
            //UserMessageStatus status = UserMessageStatus.None;
            SocketGuildChannel messageChannel = (SocketGuildChannel)socketMessage.Channel;
            ulong userid = socketMessage.Author.Id;
            //check if user sent a link, do it before the cooldown command to circumvent it
            UrlDetails postedLink = webLinkInfo.ExtractURL(socketMessage.Content, 
                new Tuple<ulong, ulong, ulong>
                    (messageChannel.Guild.Id, socketMessage.Channel.Id, socketMessage.Id));
            if (!webLinkInfo.IsUrlDetailsEmpty(postedLink))
            {
                //UrlDetails[] extractedLinksFromSameUser = G.database.SelectFrom_Table(_webLinkInfo.Domain, userid, postedLink.UrlId);
                string[] extractedMessageLinksFromDifferentUsers = database.SelectFrom_Table(webLinkInfo.Domain, postedLink.UrlId);
                //string[] combined = extractedLinksFromSameUser.Union<string>(extractedMessageLinksFromDifferentUsers).ToArray();

                //Insert after, so that we don't trigger the DDD meme on the first unique link
                int records = database.InsertInto_Table(webLinkInfo.Domain, postedLink, userid);
                if (extractedMessageLinksFromDifferentUsers.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach(string message in extractedMessageLinksFromDifferentUsers)
                    {
                        sb.AppendLine(URLCreate.DiscordTextChannel(message));
                    }
                    //await ReplyDDDMeme(socketMessage, sb.ToString());
                    //status = UserMessageStatus.TripleD;
                    hollerNHoot.Code = UserMessageStatus.TripleD;
                    hollerNHoot.Message = sb.ToString();
                }
                
                //await socketMessage.Channel.SendMessageAsync($"{_webLinkInfo.CreateDiscordTextChannelURL(link)}"); //{link.ToString()} + {socketMessage.Timestamp}
            }

            bool onCooldown = _eventCooldown.IsMessageOnCooldown(userid);
            if (onCooldown)
                return hollerNHoot;//return status; //on cooldown, but don't mark it as cooldown
            
            StringBuilder conKey = new StringBuilder();
            //single thread/core           
            //ShrekGIFs gifs = new ShrekGIFs();
            //for (int i = 0; i < gifs.SearchKeys.Length; i++)
            //{
            //    string key = gifs.SearchKeys[i];
            //    int value = Search(key, socketMessage.Content);
            //    if (value > -1)
            //        conKey.AppendLine(gifs.GetValue(key));
            //}


            ConcurrentBag<string> keys = ParallelGIFSearch(socketMessage.Content);

            foreach (string item in keys)
                conKey.AppendLine(item);

            if (conKey.Length > 0)
                await socketMessage.Channel.SendMessageAsync(conKey.ToString());
            return hollerNHoot;
        }

        private async Task<DDDReply> PictureRecieved(SocketMessage socketMessage)
        {       
            DDDReply kingOfTheShow = new DDDReply();
            bool onCooldown = _eventCooldown.IsImageOnCooldown(socketMessage.Author.Id);
            if (onCooldown)
            {
                kingOfTheShow.Code = UserMessageStatus.OnCooldown;
                return kingOfTheShow;
            }

            SwampDB database = new SwampDB();
            ImageComparison farquaad = new ImageComparison();
            IEnumerable<Attachment> attachments = socketMessage.Attachments;
            HttpClient _httpClient = new HttpClient();

            //we insert after verifying all the hashes, because there's a chance that
            //the abomination could be in the list of attachments, which forces the whole thing to
            //be deleted and I want to minimize message link pointers to deleted messages 
            //List<MediaDetails> imageHashesToInsert = new List<MediaDetails>(10);
            Dictionary<MediaDetails, byte> imageHashesToInsert = new Dictionary<MediaDetails, byte>(10);
            //List<MediaDetails> videoHashesToInsert = new List<MediaDetails>(10);
            Dictionary<MediaDetails, byte> videoHashesToInsert = new Dictionary<MediaDetails, byte>(10);

            byte mediaPosition = 1;
            //bulk group up a possible DM of at most 10 images, to prevent a possible rate limit from sending dms individually
            //First: Position of Image, Second: image hash
            List<Tuple<byte, ulong>> dmBulkMessage = new List<Tuple<byte, ulong>>(10);
            foreach (Attachment media in attachments)
            {
                Stream stream = await _httpClient.GetStreamAsync(media.Url);
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    //stream = ms;
                    ms.Position = 0;
                    if (farquaad.IsAttachmentAnImageType(ms) == Media.Image)
                    {
                        //check if image is the abomination or its variants                            
                        ulong hash = farquaad.DifferenceHash(ms);
                        if (farquaad.IsThisVariantOfAbomination(hash))
                        {
                            kingOfTheShow.Code = UserMessageStatus.Delete;
                            return kingOfTheShow;
                        }                        
                        //next, check if this is not an exempted image, like the DDD meme
                        if (!farquaad.IsThisExemptHash(hash))
                        {
                            //if the hash is too similar to the original AND this same hash is not in the falsePositives list...
                            if (farquaad.CheckHashSimilarityToAbomination(hash) && !farquaad.IsThisAFalsePositive(hash))
                            {
                                // We don't want to delete the message, in case it's a false positive, even though I REALLY like to
                                //I could get spammed dms for this naturally but it's for the best since Drake is unpredictable
                                dmBulkMessage.Add(Tuple.Create(mediaPosition, hash));
                            }
                            else //if everything checks out, we insert the hash into the standby list
                            {
                                MediaDetails insert = new MediaDetails
                                    (hash, URLCreate.PartialDiscordTextChannel(socketMessage));
                                //imageHashesToInsert.Add(new MediaDetails
                                //    (hash, URLCreate.PartialDiscordTextChannel(socketMessage)));
                                if (!imageHashesToInsert.ContainsKey(insert))
                                    imageHashesToInsert[insert] = 1;
                                else
                                    imageHashesToInsert[insert]++;
                            }                            
                        }
                        else if(farquaad.IsThisAFalsePositive(hash))
                        {
                            //imageHashesToInsert.Add(new MediaDetails
                            //    (hash, URLCreate.PartialDiscordTextChannel(socketMessage)));
                            MediaDetails insert = new MediaDetails
                                    (hash, URLCreate.PartialDiscordTextChannel(socketMessage));
                            if (!imageHashesToInsert.ContainsKey(insert))
                                imageHashesToInsert[insert] = 1;
                            else
                                imageHashesToInsert[insert]++; //count occurences rather than add same occurence to list
                        }
                        /*Alternatively, I could be strict and delete any image with a score of 90.0 or better, but not all images
                         should be deleted in a server of friends whom I've known for over 10 years. Things will slip by and that's okay
                        This model is probably not sustainable, but I built this bot with a promise to Drake to get it finished back in
                        2019/2020, which I did at one point but I wanted to do more for myself.*/
                    }
                    else if (farquaad.IsAttachmentAVideoType(ms) == Media.Video)
                    {
                        //TODO: implement video hashing here
                    }
                }

                await stream.DisposeAsync();
                mediaPosition++;
            }

            if (dmBulkMessage.Count > 0)
            {
                IUser skyguys = _client.GetUserAsync(148569781761605632).Result;
                IDMChannel dmChannel = await skyguys.CreateDMChannelAsync();
                string messageLink = URLCreate.DiscordTextChannel(socketMessage);
                StringBuilder dmMessage = new StringBuilder($"Yo, Swamphead, I think Donkey found a secret entrance to my swamp! At {messageLink}\n");
                for (int i = 0; i < dmBulkMessage.Count; i++)
                {
                    dmMessage.AppendLine($"Position {dmBulkMessage[i].Item1}, Hash: {dmBulkMessage[i].Item2}");
                }
                await dmChannel.SendMessageAsync(dmMessage.ToString());
            }

            //lastly, check for duplicates
            //insert results into database, may be slow due to opening and closing connections per iteration.
            //The main loop, whether one whole or the sum of parts will take at most 10 iterations
            //The inner loop will take at most 5 iterations, so the max steps is 50 iterations
            //let's not forget about the DifferenceHash algorithm
            //the image is shrunk to a 9x8 matrix before pixel procession
            //or O(n * m)
            StringBuilder possibleReply = new StringBuilder();

            //foreach (KeyValuePair<MediaDetails, byte> image in imageHashesToInsert)
            //{
            //    string[] duplicates = database.SelectFrom_Table(Media.Image, image.Key.Hash);
            //    if (duplicates.Length > 0)
            //    {
            //        for (int i = 0; i < duplicates.Length; i++)
            //            possibleReply.AppendLine(URLCreate.DiscordTextChannel(duplicates[i]));
            //    }
            //    database.InsertInto_Table(Media.Image, image.Key, socketMessage.Author.Id);
            //}

            //foreach (KeyValuePair<MediaDetails, byte> video in videoHashesToInsert)
            //{
            //    string[] duplicates = database.SelectFrom_Table(Media.Video, video.Key.Hash);
            //    if (duplicates.Length > 0)
            //    {
            //        for (int i = 0; i < duplicates.Length; i++)
            //            possibleReply.AppendLine(URLCreate.DiscordTextChannel(duplicates[i]));
            //    }
            //    database.InsertInto_Table(Media.Video, video.Key, socketMessage.Author.Id);
            //}

            if (imageHashesToInsert.Count > 0)
                database.SelectFromAndInsertInto_Table(Media.Image, socketMessage.Author.Id, ref imageHashesToInsert, ref possibleReply);
            if (videoHashesToInsert.Count > 0)
                database.SelectFromAndInsertInto_Table(Media.Video, socketMessage.Author.Id, ref videoHashesToInsert, ref possibleReply);

            if (possibleReply.Length > 0)
            {
                //await ReplyDDDMeme(socketMessage, possibleReply.ToString());
                kingOfTheShow.Code = UserMessageStatus.TripleD;
                kingOfTheShow.Message = possibleReply.ToString();
            }               
            else
                kingOfTheShow.Code = UserMessageStatus.DoNotDelete;
            return kingOfTheShow;
        }

        private async Task ReplyDDDMeme(SocketMessage socketMessage, string message)
        {
            if (message.Length + socketMessage.Author.Username.Length + 1 > 2000) //accounting for the message, the username and the "@" symbol
                message = "HOW STUPID CAN YOU BE, DONKEY!? I can't even point to all the crap you posted before BECAUSE THERE'S SO MUCH OF IT!!!!!";
            using (FileStream fs = File.Open("ddd.jpg", FileMode.Open)) //pinging user
                await socketMessage.Channel.SendFileAsync(fs, "ddd.jpg", $"<@{socketMessage.Author.Id}>\n" + message);
        }
        private async Task _client_MessageRecieved(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook)
                return;

            //I could do database calls from a command, I'd rather not involke it here
            if (!string.IsNullOrEmpty(socketMessage.Content)) //without this nullreference check... you know the rest
            {
                //TODO: Fail if I'm using a command, but allow this to go through if there are a bunch more question marks
                if (socketMessage.Content[0] == Convert.ToChar(Config.bot.Prefix)) 
                    return;
                //if (Regex.IsMatch(socketMessage.Content, "^\\?{2, 2000}$"))
                //{

                //}
                //I think there is a loophole to avoid the database
                //check by putting the question mark in front and type the rest of the message
                //a simple workaround, would be to check if the length is exactly one...


                //I won't do this now, but it's something to think about
            }


            //Image detection is checked aganist every text channel the bot has access to because
            //Drake spams that abomination in every channel in the server
            StringBuilder dddIsTheOne = new StringBuilder();
            //if the user only sent an image
            if (socketMessage.Attachments.Count > 0)
            {
                //The order has been changed because detecting the abomination takes priority over everything else
                //if it's not in the attachments, then we do database work
                DDDReply andGiveKirbehTheBoot = await PictureRecieved(socketMessage);
                if (andGiveKirbehTheBoot.Code == UserMessageStatus.Delete)
                {
                    await socketMessage.DeleteAsync();
                    return;
                }
                else if(andGiveKirbehTheBoot.Code == UserMessageStatus.TripleD)
                {
                    dddIsTheOne.AppendLine(andGiveKirbehTheBoot.Message);
                }
                    
            }

            if (TextChannelWhiteList.ContainsId(socketMessage.Channel.Id))
            {
                if (!string.IsNullOrEmpty(socketMessage.Content)) //this stays because the user may not send any text
                {
                    //if the user is not using a command
                    //if (socketMessage.Content[0] != Convert.ToChar(Config.bot.Prefix))
                    DDDReply comingAtYa = await TextRecieved(socketMessage);
                    if (comingAtYa.Code == UserMessageStatus.TripleD)
                        dddIsTheOne.AppendLine(comingAtYa.Message);
                }
            }

            //now, we append all the copied meme links together...
            if (dddIsTheOne.Length > 0)
                await ReplyDDDMeme(socketMessage, dddIsTheOne.ToString());
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

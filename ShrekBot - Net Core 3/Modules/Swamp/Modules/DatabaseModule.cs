using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Interactivity;
using Shrekbot;
using ShrekBot.Modules.Data_Files_and_Management.Database;
using ShrekBot.Modules.Swamp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShrekBot.Modules.Swamp.Modules
{
    [RequireOwner]         //PERIODICALLY DO A CTRL + F to make sure that there are no two matching alias 
    [Remarks("the db prefix means the database is accessed, the 'iv' prefix means that the in memory storage of hashes is accessed")]
    public class DatabaseModule : ModuleBase<SocketCommandContext>
    {
        private readonly InteractivityService _interactivity;
        private readonly DiscordSocketClient _client;

        public DatabaseModule(InteractivityService service, DiscordSocketClient client)
        {
            _interactivity = service;
            _client = client; //https://stackoverflow.com/a/68326745/9521550
        }

        [Command("hash", RunMode = RunMode.Async)]
        [Summary("Runs the Difference Hash Algorithm aganist all attachments under this command")]
        public async Task HashAttachments()
        {
            IReadOnlyCollection<Attachment> attachments = Context.Message.Attachments;
            if (attachments.Count > 0)
            {
                ImageComparison media = new ImageComparison();
                HttpClient _httpClient = new HttpClient();
                StringBuilder message = new StringBuilder("**Media Hashes in order**\n");
                foreach (Attachment attachment in attachments)
                {
                    Stream stream = await _httpClient.GetStreamAsync(attachment.Url);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        if (media.IsAttachmentAnImageType(ms) == Media.Image)
                        {
                            ulong hash = media.DifferenceHash(ms);
                            message.AppendLine(hash.ToString());
                        }
                        else if (media.IsAttachmentAVideoType(ms) == Media.Video)
                        {
                            //TODO: video hashing
                        }
                    }
                }
                await ReplyAsync(message.ToString());
            }
            else
                await ReplyAsync("Donkey! There's nothing here for me to do!");
        }

        [Command("regex", RunMode = RunMode.Async)]
        [Summary("Runs the Regex operation against the inputted link under this command")]
        public async Task RegexWebLink(string link)
        {
            ExtractWebLinkInfo regex = new ExtractWebLinkInfo();
            UrlDetails details = regex.ExtractURL(link, Tuple.Create(Context.Guild.Id, Context.Channel.Id, Context.Message.Id));
            if (!details.isIdEmpty())
                await ReplyAsync(details.ToString());
            else
                await ReplyAsync("You gave me the wrong slug, Donkey!");
        }

        [Command("whitelist add", RunMode = RunMode.Async)]
        [Summary("Adds text channel to white list used for verifying if web link regex events can run")]
        [Remarks("If this command is used without an inputted channel id, then the current channel will be ADDED to white list")]
        public async Task AddToWhiteList(string chnlId = "")
        {
            const string BadResponse = "Donkey, where's the channel!? Did you peel it off like onions?";
            const string GoodResponse = "Alright Donkey, when it comes to weblinks, I'll keep an eye on this channel.";

            if (string.IsNullOrEmpty(chnlId)) //assume that the current channel will be the new white list channel
            {
                ITextChannel textChannel = Context.Channel as ITextChannel;
                TextChannelWhiteList.Add(textChannel.Id, textChannel.Name);
                await ReplyAsync(GoodResponse);
            }
            else
            {
                ulong channelId = Utilities.CheckAndConvertUInt(chnlId);
                if (channelId == 0)
                {
                    await ReplyAsync(BadResponse);
                    return;
                }
                //DiscordSocketClient socketClient = Context.Channel as DiscordSocketClient; //null reference exception
                ITextChannel textChannel = (ITextChannel)_client.GetChannel(channelId);
                if (textChannel != null)
                {
                    TextChannelWhiteList.Add(channelId, textChannel.Name);
                    await ReplyAsync(GoodResponse);
                }
                else
                {
                    await ReplyAsync(BadResponse);
                }
            }
        }

        [Command("whitelist remove", RunMode = RunMode.Async)]
        [Alias("whitelist rm")]
        [Summary("Removes a text channel from white list used for verifying if web link regex events can run")]
        [Remarks("If this command is used without an inputted channel id, " +
            "then the current channel will be REMOVED from the white list.")]
        public async Task RemoveTextChannelFromWhiteList(string chnlId = "")
        {
            const string BadResponse = "Donkey, where's the channel!? Did you peel it off like onions?";
            const string GoodResponse = "Okay, I'm no longer paying attention to this text channel!";
            const string QuestionResponse = "Uhh... can you try that again Donkey?";

            if (string.IsNullOrEmpty(chnlId))
            {
                ITextChannel textChannel = Context.Channel as ITextChannel;
                bool x = TextChannelWhiteList.Remove(textChannel.Id);
                if (x)
                    await ReplyAsync(GoodResponse);
                else
                    await ReplyAsync(QuestionResponse);
            }
            else
            {
                ulong channelId = Utilities.CheckAndConvertUInt(chnlId);
                if (channelId == 0)
                {
                    await ReplyAsync(BadResponse);
                    return;
                }
                ITextChannel textChannel = (ITextChannel)_client.GetChannel(channelId);
                if (textChannel != null)
                {
                    bool y = TextChannelWhiteList.Remove(channelId);
                    if (y)
                        await ReplyAsync(GoodResponse);
                    else
                        await ReplyAsync(QuestionResponse);
                }
                else
                {
                    await ReplyAsync(BadResponse);
                }
            }
        }

        [Command("whitelist print", RunMode = RunMode.Async)]
        [Summary("Prints the names of the text channels in the white list")]
        public async Task PrintWhiteList()
        {
            string message = TextChannelWhiteList.Print();
            await ReplyAsync(message);
        }


        [Group("db")]
        [Summary("Manages the database tables")]
        [Remarks("The db prefix is 'database'")]
        [RequireOwner]
        public class ManageDatabaseTables : ModuleBase<SocketCommandContext>
        {
            private readonly InteractivityService _interactivity;
            public ManageDatabaseTables(InteractivityService service) => _interactivity = service;

            [Command("records", RunMode = RunMode.Async)]
            [Alias("count")]
            [Summary("Gets the count of records from all tables in the database")]
            public async Task DisplayDatabaseInfo()
            {
                SwampDB swamp = new SwampDB();
                string records = swamp.SelectCountOfRecordsFromAllTables();
                await ReplyAsync(records);
            }

            [Command("delete", RunMode = RunMode.Async)]
            [Summary("Deletes a set number of web links or media hashes from a database table")]
            public async Task DeleteWebLinksOrHashesFromDB()
            {
                IMessageChannel channel = Context.Client.GetChannel(Context.Channel.Id) as IMessageChannel;

                SwampDB swamp = new SwampDB();
                EmbedBuilder build = new EmbedBuilder();
                build.WithDescription(swamp.SelectCountOfRecordsFromAllTables());

                await channel.SendMessageAsync("How many records would you like to delete? " +
                    $"Reply in {_interactivity.DefaultTimeout.Seconds} seconds",
                    false, build.Build());

                InteractivityResult<SocketMessage> nextResult2
                    = await _interactivity.NextMessageAsync(x => x.Author.Id == Context.User.Id);

                if (!nextResult2.IsSuccess)
                {
                    await channel.SendMessageAsync("You ran out of time, Donkey!");
                    return;
                }

                int recordsToDelete;

                if (!int.TryParse(nextResult2.Value.Content, out recordsToDelete))
                {
                    await channel.SendMessageAsync("This ain't even an number!!");
                    return;
                }
                else
                    recordsToDelete = Convert.ToInt32(nextResult2.Value.Content);


                if (recordsToDelete <= 0)
                {
                    await channel.SendMessageAsync("Cannot delete a negative or zero number of records");
                    return;
                }

                await channel.SendMessageAsync("Type table name IN LOWERCASE, without the _links suffix AND in SINGULAR form, from this embed as your reply. " +
                    $"Reply in {_interactivity.DefaultTimeout.Seconds} seconds");
                InteractivityResult<SocketMessage> nextResult = await _interactivity.NextMessageAsync(x => x.Author.Id == Context.User.Id);
                if (nextResult.IsSuccess)
                {
                    string tablename = nextResult.Value.Content;
                    int rowsDeleted = 0;
                    if (tablename == WebDomain.YouTube.ToString().ToLower())
                        rowsDeleted = swamp.DeleteFrom_Table(WebDomain.YouTube, recordsToDelete);

                    else if (tablename == WebDomain.Twitter.ToString().ToLower())
                        rowsDeleted = swamp.DeleteFrom_Table(WebDomain.Twitter, recordsToDelete);

                    else if (tablename == WebDomain.Reddit.ToString().ToLower())
                        rowsDeleted = swamp.DeleteFrom_Table(WebDomain.Reddit, recordsToDelete);

                    else if (tablename == Media.Image.ToString().ToLower())
                        rowsDeleted = swamp.DeleteFrom_Table(Media.Image, recordsToDelete);

                    else if (tablename == Media.Video.ToString().ToLower())
                        rowsDeleted = swamp.DeleteFrom_Table(Media.Video, recordsToDelete);
                    else
                    {
                        await channel.SendMessageAsync("That table doesn't exist!");
                        return;
                    }
                    await channel.SendMessageAsync($"Successfully deleted {rowsDeleted} records INDISCRIMINATELY");
                }
                else
                    await channel.SendMessageAsync("You ran out of time, Donkey!");
            }

            [Command("deletehash", RunMode = RunMode.Async)]
            [Summary("Deletes all records of specified hash from the database table")]
            public async Task DeleteAllRecordsOfSameHashFromDB(string hash = "")
            {
                ulong convertedHash;
                IMessageChannel channel = Context.Client.GetChannel(Context.Channel.Id) as IMessageChannel;
                if (!ulong.TryParse(hash, out convertedHash) || string.IsNullOrEmpty(hash))
                {
                    await channel.SendMessageAsync("This isn't a proper hash!");
                    return;
                }
                convertedHash = Convert.ToUInt64(hash);

                await channel.SendMessageAsync("Which table. **image** or **video**? " +
                    $"Reply in {_interactivity.DefaultTimeout.Seconds} seconds.");
                InteractivityResult<SocketMessage> nextResult
                    = await _interactivity.NextMessageAsync(x => x.Author.Id == Context.User.Id);

                if (!nextResult.IsSuccess)
                {
                    await channel.SendMessageAsync("You ran out of time, Donkey.");
                    return;
                }
                SwampDB swamp = new SwampDB();
                int recordsDeleted = 0;
                if (nextResult.Value.Content == Media.Image.ToString().ToLower())
                    recordsDeleted = swamp.DeleteHashFrom_Table(Media.Image, convertedHash);
                else if (nextResult.Value.Content == Media.Video.ToString().ToLower())
                    recordsDeleted = swamp.DeleteHashFrom_Table(Media.Video, convertedHash);
                else
                {
                    await channel.SendMessageAsync("That is not a table!");
                    return;
                }
                await channel.SendMessageAsync($"Removed {recordsDeleted} hash(es) from the table");
            }

            [Command("deleteregex", RunMode = RunMode.Async)]
            [Summary("Deletes all records of specified regex url from the database table")]
            public async Task DeleteAllRecordsOfSameRegexFromDB(string regex = "")
            {
                IMessageChannel channel = Context.Client.GetChannel(Context.Channel.Id) as IMessageChannel;
                if (string.IsNullOrEmpty(regex))
                {
                    await channel.SendMessageAsync("I don't see anything!!!");
                    return;
                }

                StringBuilder choices = new StringBuilder();
                foreach (WebDomain domain in (WebDomain[])Enum.GetValues(typeof(WebDomain)))
                {
                    if (domain != WebDomain.None)
                        choices.Append($"**{domain.ToString().ToLower()}**, ");
                }
                choices.Length -= 2;

                await channel.SendMessageAsync($"Which table: {choices.ToString()}? " +
                    $"Reply in {_interactivity.DefaultTimeout.Seconds} seconds.");
                InteractivityResult<SocketMessage> nextResult
                    = await _interactivity.NextMessageAsync(x => x.Author.Id == Context.User.Id);

                if (!nextResult.IsSuccess)
                {
                    await channel.SendMessageAsync("You ran out of time, Donkey.");
                    return;
                }
                SwampDB swamp = new SwampDB();
                int recordsDeleted = 0;
                if (nextResult.Value.Content == WebDomain.YouTube.ToString().ToLower())
                    recordsDeleted = swamp.DeleteUrlIdFrom_Table(WebDomain.YouTube, regex);

                else if (nextResult.Value.Content == WebDomain.Twitter.ToString().ToLower())
                    recordsDeleted = swamp.DeleteUrlIdFrom_Table(WebDomain.Twitter, regex);

                else if (nextResult.Value.Content == WebDomain.Reddit.ToString().ToLower())
                    recordsDeleted = swamp.DeleteUrlIdFrom_Table(WebDomain.Reddit, regex);
                else
                {
                    await channel.SendMessageAsync("That is not a table!");
                    return;
                }
                await channel.SendMessageAsync($"Removed {recordsDeleted} regex id(s) from the table");
            }

            //TODO: Create functions for getting db values by discord username
            //don't know if I want to commit to this, but the function is here if I ever want to go back
            //    [Command("select", RunMode = RunMode.Async)]
            //    [Summary("Gets the latest db records from the inputted discord user id")]
            //    public async Task GetLatestDBRecordsFromUser(string userid = "")
            //    {
            //        if(string.IsNullOrEmpty(userid))
            //        {

            //            return;
            //        }

            //        ulong discordUserId = Utilities.CheckAndConvertUInt(userid);
            //        if(discordUserId == 0)
            //        {

            //            return;
            //        }

            //        //use interactivity here, ask for all tables, I type lowercase, singular word. profit. then move to last task
            //        //the periodic message sending
            //        SwampDB swamp = new SwampDB();
            //        string name = swamp.GetFriendName(discordUserId);
            //        if(string.IsNullOrEmpty(name))
            //        {

            //            return;
            //        }
            //        EmbedBuilder build = new EmbedBuilder();
            //    }
            //}           
        }

        [Group("iv")]
        [Alias("IV")]
        [Summary("Manages the internal dictionaries used for checking certain media hashes")]
        [Remarks("'iv' is short for for ImageVideo")]
        [RequireOwner]
        public class ManageInternalHashes : ModuleBase<SocketCommandContext>
        {
            private async Task PrintToTextFile(SocketCommandContext context, string message, string filename)
            {//https://www.w3tutorials.net/blog/memorystream-cannot-access-a-closed-stream/
                StreamWriter sw = null;
                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        sw = new StreamWriter(ms);
                        sw.Write(message);
                        sw.Flush();
                        ms.Position = 0;

                        await Context.Channel.SendFileAsync(ms, filename);

                    }
                }
                finally
                {
                    if (sw != null)
                        sw.Dispose();
                }
            }

            [Command("addabomn", RunMode = RunMode.Async)]
            [Alias("aa", "aadd")]
            [Summary("The blacklist used to see if this is an abomination image")]
            public async Task AddAbominationVariant(string name = "", string abominationHashVariant = "")
            {
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(abominationHashVariant))
                {
                    await ReplyAsync("Donkey, give me something better than that! I need TWO good stuff. A name and the hash!");
                    return;
                }
                ulong abominationHashVariantConvert = Utilities.CheckAndConvertUInt(abominationHashVariant);
                if (abominationHashVariantConvert == 0)
                {
                    await ReplyAsync("Donkey, you can't give me nothing or garbage!");
                    return;
                }
                ImageComparison comparison = new ImageComparison();
                int recordsRemoved = comparison.AddAbominationVariant(abominationHashVariantConvert, name);
                string message = "";
                if (recordsRemoved > 0)
                    message = $"Donkey, {recordsRemoved} sneaky rat(s) got past me into the Swamp! I killed it though so I'll keep watch!";
                else
                    message = "I'll keep watch of this new Onion rat!";
                await ReplyAsync(message);
            }

            [Command("removeabomn", RunMode = RunMode.Async)]
            [Alias("ra", "rab")]
            [Remarks("If I call this function, then I'm the foolish one for making an earlier mistake")]
            public async Task RemoveAbominationVariant(string abominationHashVariant = "")
            {
                if (string.IsNullOrEmpty(abominationHashVariant))
                {
                    await ReplyAsync("Donkey, ya give me something better than that! Where's the hash!?");
                    return;
                }

                ulong abominationHashVariantConvert = Utilities.CheckAndConvertUInt(abominationHashVariant);
                if (abominationHashVariantConvert == 0)
                {
                    await ReplyAsync("Tell me Donkey. What am I removing again? Cuz that ain't the hash!");
                    return;
                }
                ImageComparison comparison = new ImageComparison();
                bool removed = comparison.RemoveAbominationVariant(abominationHashVariantConvert);
                string message = "";
                if (removed)
                    message = $"Donkey, I firmly believe you have too much onions in your eyes, but fine! I GUESS this is not a rat!";
                else
                    message = "Oopsy, I can't unsee a rat heheh!";
                await ReplyAsync(message);
            }

            [Command("printabomn", RunMode = RunMode.Async)]
            [Alias("printabomn", "pa")]
            [Summary("The list of abominations that Drake may try to circumvent")]
            public async Task PrintAbominationHashesAndNames()
            {
                ImageComparison comparison = new ImageComparison();
                string garbage = comparison.GetAbominationKeysAndNames();
                if (garbage.Length > 2000) //I don't care if the string looks weird here, and if I really wanted to, I could split with the split function
                {
                    //await ReplyAsync(garbage.Substring(0, 1000));
                    //await Task.Delay(1);
                    //await ReplyAsync(garbage.Substring(1001, 2000));
                    await PrintToTextFile(Context, garbage, "Abomination Hashes.txt");
                }
                else
                    await ReplyAsync(garbage);
            }

            [Command("addexempt", RunMode = RunMode.Async)]
            [Alias("ae")]
            [Summary("Images that are not to be added to database")]
            public async Task AddExemptImageHash(string name = "", string exemptHash = "")
            {
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(exemptHash))
                {
                    await ReplyAsync("Donkey, give me something better than that! I need TWO good stuff. A name and the hash!");
                    return;
                }
                ulong exemptHashConvert = Utilities.CheckAndConvertUInt(exemptHash);
                if (exemptHashConvert == 0)
                {
                    await ReplyAsync("Donkey, you can't give me nothing or garbage!");
                    return;
                }

                ImageComparison comparison = new ImageComparison();
                int recordsRemoved = comparison.AddExemptHash(exemptHashConvert, name);
                string message = "";
                if (recordsRemoved > 0)
                    message = $"Donkey, how did you mistake {recordsRemoved} non-rat(s) AS rat(s). I don't mind 'em, but they shouldn't be here!";
                else
                    message = "Okay, a new non-rat not to be entered into my Swamp!";
                await ReplyAsync(message);
            }

            [Command("removeexempt", RunMode = RunMode.Async)]
            [Alias("re")]
            [Remarks("It's unlikely that I'll call this method as well, if I know that certain reaction images will not be added")]
            public async Task RemoveExemptImageHash(string exemptHash = "")
            {
                if (string.IsNullOrEmpty(exemptHash))
                {
                    await ReplyAsync("Donkey, ya give me something better than that! Where's the hash!?");
                    return;
                }

                ulong exemptHashConvert = Utilities.CheckAndConvertUInt(exemptHash);
                if (exemptHashConvert == 0)
                {
                    await ReplyAsync("Tell me Donkey. What am I removing again? Cuz that ain't the hash!");
                    return;
                }
                ImageComparison comparison = new ImageComparison();
                bool removed = comparison.RemoveExemptHash(exemptHashConvert);
                string message = "";
                if (removed)
                    message = $"So what you're saying Donkey is that this is an actual rat to keep out ehh?!";
                else
                    message = "Oopsy, I can't unsee a NON-rat heheh!";
                await ReplyAsync(message);
            }

            [Command("printexempt", RunMode = RunMode.Async)]
            [Alias("pe")]
            [Summary("The list of exempt images that should not be inserted into database")]
            public async Task PrintExemptImageHashes()
            {
                ImageComparison comparison = new ImageComparison();
                string trash = comparison.GetExemptKeysAndNames();
                if (trash.Length > 2000)
                {
                    await PrintToTextFile(Context, trash, "Exempt Hashes.txt");
                }
                else
                    await ReplyAsync(trash);
            }

            [Command("addfalse", RunMode = RunMode.Async)]
            [Alias("af")]
            [Remarks("Use after visually verifying the false positive is in fact, a false positive")]
            public async Task AddFalsePositiveImageHash(string name = "", string falsePositiveHash = "")
            {
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(falsePositiveHash))
                {
                    await ReplyAsync("Donkey, give me something better than that! I need TWO good stuff. A name and the hash!");
                    return;
                }
                ulong falsePositiveHashConvert = Utilities.CheckAndConvertUInt(falsePositiveHash);
                if (falsePositiveHashConvert == 0)
                {
                    await ReplyAsync("Donkey, you can't give me nothing or garbage!");
                    return;
                }

                ImageComparison comparison = new ImageComparison();
                comparison.AddFalsePositive(falsePositiveHashConvert, name);
                await ReplyAsync("So what you're saying is that this is NOT a rat? Got it!");
            }

            [Command("removefalse", RunMode = RunMode.Async)]
            [Alias("rf")]
            [Remarks("As usually, it's very unlikely that this function will be called after checking that it is not an abomination")]
            public async Task RemoveFalsePositiveImageHash(string falsePostiveHash = "")
            {
                if (string.IsNullOrEmpty(falsePostiveHash))
                {
                    await ReplyAsync("Donkey, ya give me something better than that! Where's the hash!?");
                    return;
                }

                ulong falsePositiveHashConvert = Utilities.CheckAndConvertUInt(falsePostiveHash);
                if (falsePositiveHashConvert == 0)
                {
                    await ReplyAsync("Tell me Donkey. What am I removing again? Cuz that ain't the hash!");
                    return;
                }

                ImageComparison comparison = new ImageComparison();
                bool removed = comparison.RemoveFalsePositiveHash(falsePositiveHashConvert);
                string message = "";
                if (removed)
                    message = $"Hold up. THAT WAS A RAT ALL ALONG!? DONKEY, YOU--";
                else
                    message = "What are you doing? This is fine, I think!";
                await ReplyAsync(message);
            }

            [Command("printfalse", RunMode = RunMode.Async)]
            [Alias("pf")]
            [Summary("The list of false positives that can be inserted into database")]
            public async Task PrintFalsePositiveImageHashes()
            {
                ImageComparison comparison = new ImageComparison();
                string filth = comparison.GetFalsePositiveKeysAndNames();
                if (filth.Length > 2000)
                {
                    //using (MemoryStream ms = new MemoryStream())
                    //{
                    //    using (StreamWriter outputFile = new StreamWriter(ms))
                    //    {
                    //        ms.Position = 0;
                    //        outputFile.Write(filth);
                    //        //FileAttachment attachment = new FileAttachment(outputFile)
                    //        //using(FileStream fs = new FileStream())
                    //        await Context.Channel.SendFileAsync(ms, "False Positives.txt");

                    //    }
                    //}
                    await PrintToTextFile(Context, filth, "False Positives.txt");
                }
                else
                    await ReplyAsync(filth);
            }
        }
    }
}

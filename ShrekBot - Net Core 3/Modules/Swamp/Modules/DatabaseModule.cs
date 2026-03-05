using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Interactivity;
using ShrekBot.Modules.Data_Files_and_Management.Database;
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

namespace ShrekBot.Modules.Swamp.Modules
{
    [RequireOwner]         //PERIODICALLY DO A CTRL + F to make sure that there are no two matching alias 
    [Remarks("the db prefix means the database is accessed, the 'iv' prefix means that the in memory storage of hashes is accessed")]
    public class DatabaseModule : ModuleBase<SocketCommandContext>
    {

        private readonly InteractivityService _interactivity;

        public DatabaseModule(InteractivityService service) => _interactivity = service;

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

        [Command("dbrecords", RunMode = RunMode.Async)]
        [Alias("dbcount")]
        [Summary("Gets the count of records from all tables in the database")]
        public async Task DisplayDatabaseInfo()
        {
            //ImageComparison compare = new ImageComparison();
            //compare.AddFalsePositive(12729684797621830272, "kirbeeO.png");
            //await ReplyAsync("Gottem");
            SwampDB swamp = new SwampDB();
            string records = swamp.SelectCountOfRecordsFromAllTables();
            await ReplyAsync(records);
        }

        [Command("dbdelete", RunMode = RunMode.Async)]
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

            if(!nextResult2.IsSuccess)
            {
                await channel.SendMessageAsync("You ran out of time, Donkey!");
                return;
            }

            int recordsToDelete;//= Convert.ToInt32(nextResult2.Value.Content);

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

        //TODO: Add function here and in DB code to delete all records by hash or regex id

        
        [Command("ivaddabomn", RunMode = RunMode.Async)]
        [Alias("ivaa", "ivaadd")]
        [Summary("The blacklist used to see if this is an abomination image")]
        public async Task AddAbominationVariant(string name, ulong abominationHashVariant)
        {
            ImageComparison comparison = new ImageComparison();
            int recordsRemoved = comparison.AddAbominationVariant(abominationHashVariant, name);
            string message = "";
            if (recordsRemoved > 0)
                message = $"Donkey, {recordsRemoved} sneaky rat(s) got past me into the Swamp! I killed it though so I'll keep watch!";
            else
                message = "I'll keep watch of this new Onion rat!";
            await ReplyAsync(message);
        }

        [Command("ivremoveabomn", RunMode = RunMode.Async)]
        [Alias("ivra", "ivrab")]
        [Remarks("If I call this function, then I'm the foolish one for making an earlier mistake")]
        public async Task RemoveAbominationVariant(ulong abominationHashVariant)
        {
            ImageComparison comparison = new ImageComparison();
            bool removed = comparison.RemoveAbominationVariant(abominationHashVariant);
            string message = "";
            if (removed)
                message = $"Donkey, I firmly believe you have too much onions in your eyes, but fine! I GUESS this is not a rat!";
            else
                message = "Oopsy, I can't unsee a rat heheh!";
            await ReplyAsync(message);
        }

        [Command("ivprintabomn", RunMode = RunMode.Async)]
        [Alias("printabomn", "ivpa")]
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

        [Command("ivaddexempt", RunMode = RunMode.Async)]
        [Alias("ivae")]
        [Summary("Images that are not to be added to database")]
        public async Task AddExemptImageHash(string name, ulong exemptHash)
        {
            ImageComparison comparison = new ImageComparison();
            int recordsRemoved = comparison.AddExemptHash(exemptHash, name);
            string message = "";
            if (recordsRemoved > 0)
                message = $"Donkey, how did you mistake {recordsRemoved} non-rat(s) AS rat(s). I don't mind 'em, but they shouldn't be here!";
            else
                message = "Okay, a new non-rat not to be entered into my Swamp!";
            await ReplyAsync(message);
        }

        [Command("ivremoveexempt", RunMode = RunMode.Async)]
        [Alias("ivre")]
        [Remarks("It's unlikely that I'll call this method as well, if I know that certain reaction images will not be added")]
        public async Task RemoveExemptImageHash(ulong exemptHash)
        {
            ImageComparison comparison = new ImageComparison();
            bool removed = comparison.RemoveExemptHash(exemptHash);
            string message = "";
            if (removed)
                message = $"So what you're saying Donkey is that this is an actual rat to keep out ehh?!";
            else
                message = "Oopsy, I can't unsee a NON-rat heheh!";
            await ReplyAsync(message);
        }

        [Command("ivprintexempt", RunMode = RunMode.Async)]
        [Alias("ivpe")]
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

        [Command("ivaddfalse", RunMode = RunMode.Async)]
        [Alias("ivaf")]
        [Remarks("Use after visually verifying the false positive is in fact, a false positive")]
        public async Task AddFalsePositiveImageHash(string name, ulong falsePositiveHash)
        {
            ImageComparison comparison = new ImageComparison();
            comparison.AddFalsePositive(falsePositiveHash, name);
            await ReplyAsync("So what you're saying is that this is NOT a rat? Got it!");
        }

        [Command("ivremovefalse", RunMode = RunMode.Async)]
        [Alias("ivrf")]
        [Remarks("As usually, it's very unlikely that this function will be called after checking that it is not an abomination")]
        public async Task RemoveFalsePositiveImageHash(ulong falsePostiveHash)
        {
            ImageComparison comparison = new ImageComparison();
            bool removed = comparison.RemoveFalsePositiveHash(falsePostiveHash);
            string message = "";
            if (removed)
                message = $"Hold up. THAT WAS A RAT ALL ALONG!? DONKEY, YOU--";
            else
                message = "What are you doing? This is fine, I think!";
            await ReplyAsync(message);
        }

        [Command("ivprintfalse", RunMode = RunMode.Async)]
        [Alias("ivpf")]
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

        

        //ADD MORE COMMANDS TOMMOROW
    }
}

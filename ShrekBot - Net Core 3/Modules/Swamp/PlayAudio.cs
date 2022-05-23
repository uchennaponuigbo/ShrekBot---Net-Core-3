using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ShrekBot.Modules.Data_Files_and_Management;
using Victoria;
using Victoria.Enums;

namespace ShrekBot.Modules.Swamp
{
    public class PlayAudio : ModuleBase<SocketCommandContext> //ICommandContext
    {
        //public AudioService Audio { get; set; } //dependency injection
        private readonly LavaNode _lavaNode;
        private const string JoinVC = "Donkey!! Get in a voice channel!";
        private const string PlayWhileInVC = "I ain't playing a track without you here, Donkey!";

        public PlayAudio(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;  //null, find a fix
        }

        private async Task PlaySong(string query)
        {
            var searchResponse = await _lavaNode.SearchYouTubeAsync(query);
            if (searchResponse.LoadStatus == LoadStatus.LoadFailed ||
                 searchResponse.LoadStatus == LoadStatus.NoMatches)
            {
                await ReplyAsync($"Donkey! Where is the mixtape for `{query}`!?");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (player.PlayerState == PlayerState.Playing)
            {
                await ReplyAsync("DONKEY!! LET ME FINISH THIS SONG!!");
                return;
            }
            else
            {
                var track = searchResponse.Tracks[0];
                await player.PlayAsync(track);
                await ReplyAsync($"Start playing {track.Title}, Donkey!");
            }
        }

        [Command("join")] [RequireOwner]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.Connect, ErrorMessage = "Unable to Join!!")]
        [RequireUserPermission(GuildPermission.Connect, ErrorMessage = "Cannot Join!")]
        public async Task Join()
        {
            if (_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("Donkey!! I'm already connected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync(JoinVC);
                return;
            }

            try
            {               
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"I'm at {voiceState.VoiceChannel.Name}, Donkey!");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("leave")]
        [RequireContext(ContextType.Guild)]
        public async Task Leave()
        {
            //"How do you leave a place that you were never at, Donkey!?"
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("How do you leave a place that you were never at, Donkey!?");
                return;
            }
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You're not in the swamp yourself!");
                return;
            }
            

            //await ReplyAsync("You can't tell me to leave if you aren't even with me!");
            await ReplyAsync("Fine! I'm leaving!");
            await _lavaNode.LeaveAsync(voiceState.VoiceChannel);
        }

        [Command("shrek")]
        [Alias("all", "star", "allstar")]
        [RequireContext(ContextType.Guild)]
        [Summary("Shrek's Theme. (It could take a few seconds for the song to play)")]
        public async Task PlayAllStar() 
        {
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync(PlayWhileInVC);
                return;
            }
            await PlaySong(JSONUtilities.GetAlert("allstar"));
            
        }

        [Command("shrek2")]
        [RequireContext(ContextType.Guild)]
        [Alias("hero")]
        [Summary("Infamous song in Shrek 2. (It could take a few seconds for the song to play)")]
        public async Task PlayHero()
        {
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync(PlayWhileInVC);
                return;
            }
            await PlaySong(JSONUtilities.GetAlert("hero"));
        }
    }
}

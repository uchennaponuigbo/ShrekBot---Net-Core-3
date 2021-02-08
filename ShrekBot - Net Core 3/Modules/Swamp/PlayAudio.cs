using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using ShrekBot.Modules.Data_Files_and_Management;

namespace ShrekBot.Modules.Swamp
{
    public class PlayAudio : ModuleBase<SocketCommandContext> //ICommandContext
    {
        public AudioService Audio { get; set; } //dependency injection

        [Command("join", RunMode = RunMode.Async)] [RequireOwner]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.Connect, ErrorMessage = "Unable to Join!!")]
        [RequireUserPermission(GuildPermission.Connect, ErrorMessage = "Cannot Join!")]
        public async Task Join(/*ulong vc = 0,*/ /*IVoiceChannel channel = null*/)
        {
            //if (vc != 0)
            //{
            //    Discord.WebSocket.SocketVoiceChannel user = Context.Guild.GetVoiceChannel(vc); //cannot convert from ulong? to ulong
            //    channel = user as IVoiceChannel; 
            //}
            await Audio.ConnecttoVC(Context);           
        }

        [Command("leave", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task Leave()
        {            
            await Audio.Leave(Context, true);
        }

        [Command("shrek", RunMode = RunMode.Async)]
        [Alias("all", "star", "allstar")]
        [RequireContext(ContextType.Guild)]
        [Summary("Shrek's Theme. (It could take a few seconds for the song to play)")]
        public async Task PlayAllStar() 
        {   
            if(!Audio.MusicPlaying)
            {                
                await Audio.ConnectAndPlay(Context, JSONUtilities.GetAlert("allstar"));
            }
            else
                await ReplyAsync(Audio.notFinished);
        }

        [Command("shrek2", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [Alias("hero")]
        [Summary("Infamous song in Shrek 2. (It could take a few seconds for the song to play)")]
        public async Task PlayHero()
        {
            if (!Audio.MusicPlaying)
            {
                await Audio.ConnectAndPlay(Context, JSONUtilities.GetAlert("hero"), 0);
            }
            else
                await ReplyAsync(Audio.notFinished);
        }
    }
}

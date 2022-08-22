using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ShrekBot.Modules.Data_Files_and_Management;

namespace ShrekBot.Modules.Swamp
{
    public class PlayAudio : ModuleBase<SocketCommandContext> 
    {
        private readonly AudioService _audio;
        public PlayAudio(AudioService audio)
        {
            _audio = audio;
        }

        [Command("join", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task JoinVCAsync(IVoiceChannel channel = null)
        {
            await _audio.ConnecttoVC(Context);        
        }

        [Command("leave", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task LeaveVCAsync(IVoiceChannel channel = null)
        {
            await _audio.Leave(Context, true);

        }

        [Command("shrek", RunMode = RunMode.Async)]
        [Alias("all", "star", "allstar")]
        [RequireContext(ContextType.Guild)]
        [RequireOwner] //for now...
        [Summary("Shrek's Theme. (It could take a few seconds for the song to play)")]
        public async Task PlayAllStarAsync(IVoiceChannel channel = null)
        {
            ShrekSongs song = new ShrekSongs();
            await _audio.ConnectAndPlay(Context, song.GetValue("allstar"));  
        }

        [Command("shrek2", RunMode = RunMode.Async)]
        [Alias("hero")]
        [RequireContext(ContextType.Guild)]
        [RequireOwner] //for now...
        [Summary("I need a Hero. (It could take a few seconds for the song to play)")]
        public async Task PlayHeroAsync(IVoiceChannel channel = null)
        {
            ShrekSongs song = new ShrekSongs();
            await _audio.ConnectAndPlay(Context, song.GetValue("hero"));
        }
    }
}

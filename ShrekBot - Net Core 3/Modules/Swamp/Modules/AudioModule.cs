using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ShrekBot.Modules.Data_Files_and_Management;
using ShrekBot.Modules.Swamp.Services;

namespace ShrekBot.Modules.Swamp.Modules
{
    public class AudioModule : ModuleBase<SocketCommandContext>
    {
        //private readonly AudioService _audio;
        //public AudioModule(AudioService audio)
        //{
        //    _audio = audio;
        //}
        private readonly AudioService _audio;
        public AudioModule(AudioService audio)
        {
            _audio = audio;
        }


        [Command("join", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task JoinVCAsync(IVoiceChannel channel = null)
        {
            channel = (Context.User as IVoiceState).VoiceChannel;
            //await _audio.ConnecttoVC(Context);
            //await _audio.ConnectVCversion2(Context, (Context.User as IVoiceState).VoiceChannel);
            await _audio.ConnectToVCAsync(Context, channel);
        }
        //come here later...
        //https://discord.com/channels/81384788765712384/381889909113225237/1185949406550315039
        [Command("leave", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public async Task LeaveVCAsync(/*IVoiceChannel channel = null*/)
        {
            //await _audio.Leave(Context, true);
            await _audio.LeaveVCAsync(Context);
        }

        [Command("shrek", RunMode = RunMode.Async)]
        [Alias("all", "star", "allstar")]
        [RequireContext(ContextType.Guild)]
        [RequireOwner] //for now...
        [Summary("Shrek's Theme. (It could take a few seconds for the song to play)")]
        public async Task PlayAllStarAsync(IVoiceChannel channel = null)
        {
            ShrekSongs song = new ShrekSongs();
            //await _audio.ConnectAndPlay(Context, song.GetValue("allstar"));
        }

        [Command("shrek2", RunMode = RunMode.Async)]
        [Alias("hero")]
        [RequireContext(ContextType.Guild)]
        [RequireOwner] //for now...
        [Summary("I need a Hero. (It could take a few seconds for the song to play)")]
        public async Task PlayHeroAsync(IVoiceChannel channel = null)
        {
            ShrekSongs song = new ShrekSongs();
            //await _audio.ConnectAndPlay(Context, song.GetValue("hero"));
        }
    }
}

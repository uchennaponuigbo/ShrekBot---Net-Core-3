using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Audio;
using ShrekBot.Modules.Data_Files_and_Management;

namespace ShrekBot.Modules.Swamp.Services
{
    public class AudioService
    {
        //https://github.com/Yucked/Victoria/blob/v5/docs/guides/snippets/AudioModule.cs
        //https://github.com/Yucked/Victoria/blob/v5/docs/guides/snippets/AudioService.cs
        //https://github.com/Yucked/Victoria/tree/v5/docs/guides

        //https://github.com/domnguyen/Discord-Bot/blob/master/src/Services/AudioService.cs
        //https://github.com/domnguyen/Discord-Bot/blob/master/src/Modules/AudioModule.cs
        private const string JoinVC = "Donkey!! Get in a voice channel!";
        //    private const string PlayWhileInVC = "I ain't playing a track without you here, Donkey!";

        private const string BotAlreadyConnected = "Donkey!! I'm already connected to a voice channel!";
        //private const string UserNotConnected = "Donkey, you fool! You're not even in a voice channel!";
        private bool voiceCheck;
        private bool delayVoiceChannelAction;
        private int delayActionLength = 6000;

        private AudioPlayer audioPlayer = null;

        public AudioService()
        {
            voiceCheck = false;
            delayVoiceChannelAction = false;
            audioPlayer = new AudioPlayer();
        }

        //SEMAPHORE for delaying spam joins and leaves
        private async Task DelayChannelAction(Action f)
        {
            delayVoiceChannelAction = true; // Lock.
            f();
            await Task.Delay(delayActionLength); // Delay to prevent error condition. TEMPORARY.
            delayVoiceChannelAction = false; // Unlock.
        }

        // Gets m_DelayAction, this is a temporary semaphore to prevent joining too quickly after leaving a channel.
        //public bool GetDelayAction()
        //{
        //    if (m_DelayAction) Log("This action is delayed. Please try again later.");
        //    return m_DelayAction;
        //}

        private IVoiceChannel GetVoiceChannel(SocketCommandContext ctx)
        {
            SocketGuildUser user = ctx.User as SocketGuildUser;
            return user.VoiceChannel;
        }

        private EmbedBuilder Builder(string message)
        {
            return new EmbedBuilder()
            {
                Color = Color.Magenta,
                Description = message
            };
        }

        public async Task ConnectVCversion2(SocketCommandContext context, IVoiceChannel voiceChannel)
        {
            //IGuild == Context.Guild
            if (context.Guild == null || voiceChannel == null) 
                return;

            if(delayVoiceChannelAction) //delay quick joins/leaves
            {
                await context.Channel.SendMessageAsync("", false,
                    Builder("DONKEY!! CHILL YOURSELF!").Build());
                return;
            }

            //check if bot is already joined
            if(voiceCheck)
            {
                await context.Channel.SendMessageAsync("", false,
                    Builder(BotAlreadyConnected).Build());
                return;
            }

            try
            {
                IAudioChannel audioClient = (IAudioChannel)await voiceChannel.ConnectAsync();
            }
            catch
            {
                await context.Channel.SendMessageAsync("", false,
                    Builder("Wait... what happened to the voice channel!? DONKEY!?").Build());
            }

            voiceCheck = true;
            await context.Channel.SendMessageAsync("", false,
                Builder($"Connecting to {voiceChannel.Name}").Build());
        }

        public async Task LeaveChannelversion2(SocketCommandContext context, IVoiceChannel voiceChannel)
        {
            if (context.Guild == null || voiceChannel == null)
                return;

            //stop audio before leaving
            voiceCheck = false;
            if(audioPlayer.IsRunning())
                audioPlayer.Stop();
            while (audioPlayer.IsRunning()) 
                await Task.Delay(1000); // Wait until it's fully stopped.

            //await DelayChannelAction(() => audioClient.StopAsync()); // Wait until the audioClient is properly disconnected.
            return;
        }

        public async Task<IAudioClient> ConnecttoVC(SocketCommandContext ctx)
        {
            IVoiceChannel chnl = GetVoiceChannel(ctx);

            if (chnl == null)
            {
                await ctx.Channel.SendMessageAsync("", false,
                    Builder(JoinVC).Build());
                return null;
            }
            if (voiceCheck == true)
            {
                await ctx.Channel.SendMessageAsync("", false,
                    Builder(BotAlreadyConnected).Build());
                return null;
            }
            await ctx.Channel.SendMessageAsync("", false,
                Builder($"Connecting to {chnl.Name}.").Build());
            voiceCheck = true;
            return await chnl.ConnectAsync();

        }

        public async Task<IAudioClient> Leave(SocketCommandContext ctx, bool outputLeave = false)
        {
            IVoiceChannel chnl = GetVoiceChannel(ctx);
            if (chnl == null)
            {
                await ctx.Channel.SendMessageAsync("", false,
                    Builder(JoinVC).Build());
                return null;
            }
            if (voiceCheck == false)
            {
                await ctx.Channel.SendMessageAsync("", false,
                    Builder($"Donkey! Tell me how to leave without leaving!").Build());
                return null;
            }

            if (outputLeave == true)
                await ctx.Channel.SendMessageAsync("", false,
                    Builder($"Fine! I'm leaving {chnl.Name}.").Build());

            voiceCheck = false;
            //MusicPlaying = false;
            await chnl.DisconnectAsync();
            return Task.CompletedTask as IAudioClient;
        }

        //public async Task<IAudioClient> ConnectAndPlay(SocketCommandContext ctx, string url, int autoLeave = 3000)
        //{
        //    var audioClient = await ConnecttoVC(ctx);
        //    if (audioClient == null)
        //        return null;

        //    await SendAsync(audioClient, url);
        //    await Task.Delay(autoLeave);
        //    await Leave(ctx);
        //    return Task.CompletedTask as IAudioClient;
        //}

        //private Process CreateStream(string path)
        //{
        //    return Process.Start(new ProcessStartInfo
        //    {
        //        FileName = "ffmpeg",
        //        Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
        //        UseShellExecute = false,
        //        RedirectStandardOutput = true,
        //    });
        //}

        //private async Task SendAsync(IAudioClient client, string path)
        //{
        //    // Create FFmpeg using the previous example
        //    using (var ffmpeg = CreateStream(path))
        //    using (var output = ffmpeg.StandardOutput.BaseStream)
        //    using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
        //    {
        //        try { await output.CopyToAsync(discord); }
        //        //catch(Exception ex) { Console.WriteLine(ex.Message); }
        //        finally { await discord.FlushAsync(); }
        //    }
        //}
    }
}

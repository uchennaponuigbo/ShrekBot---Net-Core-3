using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;

namespace ShrekBot.Modules.Swamp
{
    public class AudioService
    {
        public readonly string notFinished;
        public bool VoiceCheck { get; private set; }
        public bool MusicPlaying { get; private set; }

        public AudioService()
        {
            VoiceCheck = false;
            MusicPlaying = false;
            notFinished = "DONKEY!! LET ME FINISH THIS SONG!!";
        }

        private IVoiceChannel GetVoiceChannel(SocketCommandContext ctx)
        {            
            SocketGuildUser user = ctx.User as SocketGuildUser;
            return user.VoiceChannel;
        }

        //public bool IsMusicPlaying() => musicPlaying;
        //public bool IsConnected() => voiceCheck;
        //public string NotFinished() => finish;

        public async Task<IAudioClient> ConnecttoVC(SocketCommandContext ctx)
        {            
            IVoiceChannel chnl = GetVoiceChannel(ctx);
            
            //need to validate if the bot has permission to join a voice chat
            if(chnl == null)
            {
                await ctx.Channel.SendMessageAsync("Donkey!! Get in a voice channel!");
                return null;
            }
            if (VoiceCheck == true)
            {
                await ctx.Channel.SendMessageAsync($"Donkey!! I'm already at {chnl.Name}, you fool!");
                return null;
            }           
            await ctx.Channel.SendMessageAsync($"I'm at {chnl.Name} Donkey!");
            VoiceCheck = true;
            return await chnl.ConnectAsync();
             
        }

        //public /*IAudioClient*/ bool IsConnected(SocketCommandContext ctx)
        //{
        //    SocketGuild socket; = socket.CurrentUser.VoiceChannel
        //    IVoiceChannel chnl = GetVoiceChannel(ctx);
        //    if (chnl != null)
        //        return true; //Check the VoiceChannel property of the SocketGuild CurrentUser. 
        //    else
        //        return false; //If it's null, the bot is not connected
        //}

        public async Task<IAudioClient> ConnectAndPlay(SocketCommandContext ctx, string url, int delay = 3000)
        {
            var audioClient = await ConnecttoVC(ctx);
            if (audioClient == null)
                return null;

            await Stream(audioClient, url.Split('&')[0]);
            await Task.Delay(delay);
            await Leave(ctx);
            return Task.CompletedTask as IAudioClient;
        }

        public async Task<IAudioClient> Leave(SocketCommandContext ctx, bool outputLeave = false)
        {
            IVoiceChannel chnl = GetVoiceChannel(ctx);
            if (chnl == null)
            {
                await ctx.Channel.SendMessageAsync("You aren't in a voice channel Donkey!!");
                return null;
            }
            if (VoiceCheck == false)
            {
                await ctx.Channel.SendMessageAsync("How do you leave a place that you were never at, Donkey!?");
                return null;
            }

            if (outputLeave == true)
                await ctx.Channel.SendMessageAsync("Fine! I'm leaving!");

            VoiceCheck = false; //should be false before leaving
            MusicPlaying = false;
            await chnl.DisconnectAsync();
            return Task.CompletedTask as IAudioClient;
        }

        public async Task Stream(IAudioClient client, string url)
        {
            MusicPlaying = true;
            using (Process ffmpeg = CreateStream(url))
            using (Stream output = ffmpeg.StandardOutput.BaseStream)
            using (AudioOutStream discord = client.CreatePCMStream(AudioApplication.Mixed, 96000))
            {
                try { await output.CopyToAsync(discord); }
                catch(ArgumentNullException e) { Console.WriteLine(e.Message); }
                finally { await discord.FlushAsync(); }
            }
        }

        private Process CreateStream(string url)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C youtube-dl.exe -o - \"{url}\" | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                //$"/C youtube-dl.exe -o - {url} | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48100 pipe:1"
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });           
        }
    }
}

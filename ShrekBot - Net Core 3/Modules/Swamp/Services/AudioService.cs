﻿using Discord;
using Discord.Audio;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShrekBot.Modules.Data_Files_and_Management;
using Discord.Commands;

namespace ShrekBot.Modules.Swamp.Services
{
    /**
     * AudioService
     * This handles the entire audio service functionality. 
     * This service used to perform all the tasks required by the module, but most have been separated
     * into helper functions.
     * 
     * AudioDownloader handles reading simple meta data from network links and local songs. If specified,
     * it'll download network songs into a default folder.
     * 
     * AudioPlayer handles the local and network streams then passes it into FFmpeg to output to the voice channel.
     * 
     * Right now the playlist is maintained in the service, but may be abstracted or moved into another
     * class in the future.
     */
    public class AudioService
    {
        // Concurrent dictionary for multithreaded environments.
        private readonly ConcurrentDictionary<ulong, IAudioClient> m_ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        // Player.
        private readonly AudioPlayer m_AudioPlayer = new AudioPlayer();

        // Private variables.
        private int m_NumPlaysCalled = 0;           // This is to check for the last 'ForcePlay' call.
        private int m_DelayActionLengthMilliseconds = 7000;    // To prevent connection issues, we set it to a fairly 'large' value.
        private bool m_DelayAction = false;         // Temporary Semaphore to control leaving and joining too quickly.
        private bool m_AutoPlay = false;            // Flag to check if autoplay is currently on or not.
        private bool m_AutoPlayRunning = false;     // Flag to check if autoplay is currently running or not. More of a 'sanity' check really.
        private bool m_AutoDownload = true;         // Flag to auto download network items in the playlist.
        private bool m_AutoStop = false;            // Flag to stop the autoplay service when we're done playing all songs in the playlist.
        private Timer m_VoiceChannelTimer = null;   // Timer to check for active users in the voice channel.
        private bool m_LeaveWhenEmpty = true;       // Flag to set up leaving the channel when there are no active users.

        // Using the flag as a semaphore, we pass in a function to lock in between it. Added for better practice.
        // Any async function that's called after this, if required can check for m_DelayAction before continuing.
        private async Task DelayChannelAction(Action f)
        {
            m_DelayAction = true; // Lock.
            f();
            await Task.Delay(m_DelayActionLengthMilliseconds); // Delay to prevent error condition. TEMPORARY.
            m_DelayAction = false; // Unlock.
        }

        private Embed Builder(string message)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = Color.Magenta,
                Description = message
            };
            return embed.Build();
        }

        // Gets m_DelayAction, this is a temporary semaphore to prevent joining too quickly after leaving a channel.
        //public bool GetDelayAction()
        //{
        //    if (m_DelayAction)
        //        await context.Channel.
        //            SendMessageAsync("", false,
        //            Builder("Hey Donkey! Slow down willya!?"));
        //    return m_DelayAction;
        //}

        /// <summary>
        /// Joins the voice channel of the target.
        /// Adds a new client to the ConcurrentDictionary.
        /// <br/>
        /// The bot will leave after a specfied time if there is no one in the VC.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="voiceChannel"></param>
        /// <returns></returns>
        public async Task ConnectToVCAsync(SocketCommandContext context, IVoiceChannel voiceChannel)
        {
            if (context.Guild == null || voiceChannel == null) 
                return;

            // Delayed join if the client recently left a voice channel. This is to prevent reconnection issues.
            if (m_DelayAction)
            {
                await context.Channel.
                    SendMessageAsync("", false, 
                    Builder("DONKEY!!! I JUST LEFT THAT CHANNEL!!!"));
                return;
            }

            // Try to get the current audio client. If it's already there, we've already joined.
            if (m_ConnectedChannels.TryGetValue(context.Guild.Id, out var connectedAudioClient))
            {
                await context.Channel.
                    SendMessageAsync("", false, Builder("Donkey! I'm already connected to a voice channel!"));
                return;
            }

            // If the target guild id doesn't match the guild id we want, return.
            // This will likely never happen, but the source message could refer to the incorrect server.
            if (voiceChannel.Guild.Id != context.Guild.Id)
            {
                await context.Channel.
                    SendMessageAsync("", false, Builder("Donkey! This is not the correct Voice Channel!"));
                return;
            }

            IAudioClient audioClient = await voiceChannel.ConnectAsync();

            try // We should put a try block in case audioClient is null or some other error occurs.
            {
                // Once connected, add it to the dictionary of connected channels.
                if (m_ConnectedChannels.TryAdd(context.Guild.Id, audioClient))
                {
                    await context.Channel.SendMessageAsync("", false,
                         Builder($"Connecting to {voiceChannel.Name}"));

                    // Start check to see if anyone is even in the channel.
                    if (m_LeaveWhenEmpty)
                        m_VoiceChannelTimer = new Timer(CheckVoiceChannelState, 
                            voiceChannel, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
                    return;
                }
            }
            catch(ArgumentNullException ex)
            {
                await context.Channel.SendMessageAsync("Unable to connect to this voice channel.", false,
                         Builder(ex.Message));
            }

            // If we can't add it to the dictionary or connecting didn't work properly, error.
            await context.Channel.SendMessageAsync("", false,
                          Builder($"Could not connect to {voiceChannel.Name}..?"));
        }

        /// <summary>
        /// Leaves the current voice channel.
        /// Removes the client from the ConcurrentDictionary.
        /// <br/>
        /// If any audio is playing, then it is stopped, then the bot leaves a second later.
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        //public async Task LeaveVCAsync(IGuild guild)
        //{
        //    if (guild == null) 
        //        return;

        //    // To avoid any issues, we stop the player before leaving the channel.
        //    if (m_AudioPlayer.IsRunning()) 
        //        StopAudio();
        //    while (m_AudioPlayer.IsRunning()) 
        //        await Task.Delay(1000); // Wait until it's fully stopped.

        //    // Attempt to remove from the current dictionary, and if removed, stop it.
        //    if (m_ConnectedChannels.TryRemove(guild.Id, out IAudioClient audioClient))
        //    {
        //        // Wait until the audioClient is properly disconnected.
        //        await DelayChannelAction(() => audioClient.StopAsync()); 
        //        return;
        //    }
        //}

        //TODO: figure out how to send a message to the channel indicating that the bot has joined and left
        //while also figuring out how to not throw an error when the auto leave functionality begins
        public async Task LeaveVCAsync(SocketCommandContext context)
        {
            if (context.Guild == null)
                return;

            // To avoid any issues, we stop the player before leaving the channel.
            if (m_AudioPlayer.IsRunning())
                StopAudio();
            while (m_AudioPlayer.IsRunning())
                await Task.Delay(1000); // Wait until it's fully stopped.

            // Attempt to remove from the current dictionary, and if removed, stop it.
            if (m_ConnectedChannels.TryRemove(context.Guild.Id, out IAudioClient audioClient))
            {
                await context.Channel.SendMessageAsync("", false,
                    Builder($"Fine! I'm leaving!"));
                // Wait until the audioClient is properly disconnected.
                await DelayChannelAction(() => audioClient.StopAsync());
                return;
            }

            await context.Channel.SendMessageAsync("", false,
                          Builder($"Donkey! I can't leave something that I never joined... " +
                          $"unless I joined and I don't know it but I doubt that..."));
        }

        /// <summary>
        /// Checks the current status of the voice channel and leaves when empty.
        /// </summary>
        /// <param name="state"></param>
        private async void CheckVoiceChannelState(object state)
        {
            // We can't check anything if the client is null.
            if (state is not IVoiceChannel channel) 
                return;

            // Check user count.
            int count = (await channel.GetUsersAsync().FlattenAsync()).Count();
            if (count < 2)
            {
                await LeaveVCAsync((SocketCommandContext)channel.Guild);
                if (m_VoiceChannelTimer != null)
                {
                    m_VoiceChannelTimer.Dispose();
                    m_VoiceChannelTimer = null;
                }
            }
        }

        // Returns the number of async calls to ForcePlayAudioSync.
        public int GetNumPlaysCalled() => m_NumPlaysCalled;

        //Force Play the current audio in the voice channel of the target.
        //TODO: Consider adding it to autoplay list if it is already playing.
        public async Task ForcePlayAudioAsync(SocketCommandContext context, IMessageChannel channel, string path)
        {
            if (context.Guild == null)
                return;

            // Get audio info.
            //AudioFile song = await GetAudioFileAsync(path);

            // We can only resume autoplay on the last 'play' wait loop. We have to check other 'play's haven't been called.
            Interlocked.Increment(ref m_NumPlaysCalled);

            // To avoid any issues, we stop any other audio running. The audioplayer will also stop the current song...
            if (m_AudioPlayer.IsRunning())
                StopAudio();
            while (m_AudioPlayer.IsRunning())
                await Task.Delay(1000);

            // Start the stream, this is the main part of 'play'
            if (m_ConnectedChannels.TryGetValue(context.Guild.Id, out var audioClient))
            {
                //Log($"Now Playing: {song.Title}", (int)E_LogOutput.Reply); // Reply in the text channel.
                //Log(song.Title, (int)E_LogOutput.Playing); // Set playing.
                //await m_AudioPlayer.Play(audioClient, song); // The song should already be identified as local or network.
                //Log(Strings.NotPlaying, (int)E_LogOutput.Playing);
            }
            else
            {
                // If we can't get it from the dictionary, we're probably not connected to it yet.
                //Log("Unable to play in the proper channel. Make sure the audio client is connected.");
            }

            // Uncount this play.
            Interlocked.Decrement(ref m_NumPlaysCalled);
        }

        //// This is for the autoplay function which waits after each playback and pulls from the playlist.
        //// Since the playlist extracts the audio information, we can safely assume that it's chosen the local
        //// if it exists, or just uses the network link.
        //public async Task AutoPlayAudioAsync(IGuild guild, IMessageChannel channel)
        //{
        //    // We can't play from an empty guild.
        //    if (guild == null) 
        //        return;

        //    if (m_AutoPlayRunning) 
        //        return; // Only allow one instance of autoplay.
        //    while (m_AutoPlayRunning = m_AutoPlay)
        //    {
        //        // If the audio player is already playing, we need to wait until it's fully finished.
        //        if (m_AudioPlayer.IsRunning()) await Task.Delay(1000);

        //        // We do some checks before entering this loop.
        //        if (m_Playlist.IsEmpty || !m_AutoPlayRunning || !m_AutoPlay) break;

        //        // If there's nothing playing, start the stream, this is the main part of 'play'
        //        if (m_ConnectedChannels.TryGetValue(guild.Id, out var audioClient))
        //        {
        //            AudioFile song = PlaylistNext(); // If null, nothing in the playlist. We can wait in this loop until there is.
        //            if (song != null)
        //            {
        //                Log($"Now Playing: {song.Title}", (int)E_LogOutput.Reply); // Reply in the text channel.
        //                Log(song.Title, (int)E_LogOutput.Playing); // Set playing.
        //                await m_AudioPlayer.Play(audioClient, song); // The song should already be identified as local or network.
        //                Log(Strings.NotPlaying, (int)E_LogOutput.Playing);
        //            }
        //            else
        //                Log($"Cannot play the audio source specified : {song}");

        //            // We do the same checks again to make sure we exit right away. May not be necessary, but let's check anyways.
        //            if (m_Playlist.IsEmpty || !m_AutoPlayRunning || !m_AutoPlay) break;

        //            // Is null or done with playback.
        //            continue;
        //        }

        //        // If we can't get it from the dictionary, we're probably not connected to it yet.
        //        Log("Unable to play in the proper channel. Make sure the audio client is connected.");
        //        break;
        //    }

        //    // Stops autoplay once we're done with it.
        //    if (m_AutoStop) m_AutoPlay = false;
        //    m_AutoPlayRunning = false;
        //}

        // Returns if the audio player is currently playing or not.
        public bool IsAudioPlaying() => m_AudioPlayer.IsPlaying(); 

        // AudioPlayback Functions. Pause, Resume, Stop, AdjustVolume.
        public void PauseAudio() => m_AudioPlayer.Pause();
        public void ResumeAudio() => m_AudioPlayer.Resume();
        public void StopAudio() 
        {
            m_AutoPlay = false;
            m_AutoPlayRunning = false; 
            m_AudioPlayer.Stop(); 
        }
        /// <summary>
        /// Takes in a value from [0.0f - 1.0f]
        /// </summary>
        /// <param name="volume"></param>
        public void AdjustVolume(float volume) => m_AudioPlayer.AdjustVolume(volume);

        //// Sets the autoplay service to be true. Likely, wherever this is set, we also check and start auto play.
        //public void SetAutoPlay(bool enable) { m_AutoPlay = enable; }

        // Returns the current state of the autoplay service.
        /*public bool GetAutoPlay() { return m_AutoPlay; }*/

        // Checks if autoplay is true, but not started yet. If not started, we start autoplay here.
        //public async Task CheckAutoPlayAsync(IGuild guild, IMessageChannel channel)
        //{
        //    if (m_AutoPlay && !m_AutoPlayRunning && !m_AudioPlayer.IsRunning()) // if autoplay or force play isn't playing.
        //        await AutoPlayAudioAsync(guild, channel);
        //}

        // Prints the playlist information.
        //public void PrintPlaylist()
        //{
        //    // If none, we return.
        //    int count = m_Playlist.Count;
        //    if (count == 0)
        //    {
        //        Log("There are currently no items in the playlist.", (int)E_LogOutput.Reply);
        //        return;
        //    }

        //    // Count the number of total digits.
        //    int countDigits = (int)(Math.Floor(Math.Log10(count) + 1));

        //    // Create an embed builder.
        //    var emb = new EmbedBuilder();

        //    for (int i = 0; i < count; i++)
        //    {
        //        // Prepend 0's so it matches in length.
        //        string zeros = "";
        //        int numDigits = (i == 0) ? 1 : (int)(Math.Floor(Math.Log10(i) + 1));
        //        while (numDigits < countDigits)
        //        {
        //            zeros += "0";
        //            ++numDigits;
        //        }

        //        // Filename.
        //        AudioFile current = m_Playlist.ElementAt(i);
        //        emb.AddField(zeros + i, current);
        //    }

        //    DiscordReply("Playlist", emb);
        //}

        // Adds a song to the playlist.
        //public async Task PlaylistAddAsync(string path)
        //{
        //    // Get audio info.
        //    AudioFile audio = await GetAudioFileAsync(path);
        //    if (audio != null)
        //    {
        //        m_Playlist.Enqueue(audio); // Only add if there's no errors.
        //        Log($"Added to playlist : {audio.Title}", (int)E_LogOutput.Reply);

        //        // If the downloader is set to true, we start the autodownload helper.
        //        if (m_AutoDownload)
        //        {
        //            if (audio.IsNetwork) m_AudioDownloader.Push(audio); // Auto download while in playlist.
        //            await m_AudioDownloader.StartDownloadAsync(); // Start the downloader if it's off.
        //        }
        //    }
        //}

        // Gets the next song in the playlist queue.
        //private AudioFile PlaylistNext()
        //{
        //    if (m_Playlist.TryDequeue(out AudioFile nextSong))
        //        return nextSong;

        //    if (m_Playlist.Count <= 0) Log("We reached the end of the playlist.");
        //    else Log("The next song could not be opened.");
        //    return nextSong;
        //}

        // Skips the current playlist song if autoplay is on.
        //public void PlaylistSkip()
        //{
        //    if (!m_AutoPlay)
        //    {
        //        Log("Autoplay service hasn't been started.");
        //        return;
        //    }
        //    if (!m_AudioPlayer.IsRunning())
        //    {
        //        Log("There's no audio currently playing.");
        //        return;
        //    }
        //    m_AudioPlayer.Stop();
        //}

        // Extracts simple meta data from the path and fills a new AudioFile
        // information about the audio source. If it fails in the downloader or here,
        // we simply return null.
        //private async Task<AudioFile> GetAudioFileAsync(string path)
        //{
        //    try // We put this in a try catch block.
        //    {
        //        AudioFile song = await m_AudioDownloader.GetAudioFileInfo(path);
        //        if (song != null) // We check for a local available version.
        //        {
        //            string filename = m_AudioDownloader.GetItem(song.Title);
        //            if (filename != null) // We found a local version.
        //            {
        //                song.FileName = filename;
        //                song.IsNetwork = false; // Network is now false.
        //                song.IsDownloaded = true;
        //            }
        //        }
        //        return song;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        // Finds all the local songs and prints out a set at a time by page number.
        //public void PrintLocalSongs(int page)
        //{
        //    // Get all the songs in this directory.
        //    string[] items = m_AudioDownloader.GetAllItems();
        //    int itemCount = items.Length;
        //    if (itemCount == 0)
        //    {
        //        Log("No local files found.", (int)E_LogOutput.Reply);
        //        return;
        //    }

        //    // Count the number of total digits.
        //    int countDigits = (int)(Math.Floor(Math.Log10(items.Length) + 1));

        //    // Set pages to print.
        //    int pageSize = 20;
        //    int pages = (itemCount / pageSize) + 1;
        //    if (page < 1 || page > pages)
        //    {
        //        Log($"There are {pages} pages. Select page 1 to {pages}.", (int)E_LogOutput.Reply);
        //        return;
        //    }

        //    // Start printing.
        //    for (int p = page - 1; p < page; p++)
        //    {
        //        // Create an embed builder.
        //        var emb = new EmbedBuilder();

        //        for (int i = 0; i < pageSize; i++)
        //        {
        //            // Get the index for the file.
        //            int index = (p * pageSize) + i;
        //            if (index >= itemCount) break;

        //            // Prepend 0's so it matches in length. This will be the 'index'.
        //            string zeros = "";
        //            int numDigits = (index == 0) ? 1 : (int)(Math.Floor(Math.Log10(index) + 1));
        //            while (numDigits < countDigits)
        //            {
        //                zeros += "0";
        //                ++numDigits;
        //            }

        //            // Filename.
        //            string file = items[index].Split(Path.DirectorySeparatorChar).Last(); // Get just the file name.
        //            emb.AddField(zeros + index, file);
        //        }

        //        DiscordReply($"Page {p+1}", emb);
        //    }
        //}

        // Sets the audio download path. This should only be called during init.
        //public void SetDownloadPath(string path) { m_AudioDownloader.SetDownloadPath(path);}

        // Returns the name with the specified song by index.
        // Returns null if a local song doesn't exist.
       // public string GetLocalSong(int index) { return m_AudioDownloader.GetItem(index); }

        // Adds a song to the download queue.
        //public async Task DownloadSongAsync(string path)
        //{
        //    AudioFile audio = await GetAudioFileAsync(path);
        //    if (audio != null)
        //    {
        //        Log($"Added to the download queue : {audio.Title}", (int)E_LogOutput.Reply);

        //        // If the downloader is set to true, we start the autodownload helper.
        //        if (audio.IsNetwork) m_AudioDownloader.Push(audio); // Auto download while in playlist.
        //        await m_AudioDownloader.StartDownloadAsync(); // Start the downloader if it's off.
        //    }
        //}

        // Removes any duplicates in our download folder.
        //public async Task RemoveDuplicateSongsAsync()
        //{
        //    m_AudioDownloader.RemoveDuplicateItems();
        //    await Task.Delay(0);
        //}
    }
}

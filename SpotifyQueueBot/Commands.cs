using Discord.Commands;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace blurr.spotifybot
{
    public class Commands : ModuleBase
    {
        [Command("addsong")]
        [Alias("+")]
        private async Task AddSongAsync([Remainder] string song = "")
        {
            if (!SCommandHandler.Ready())
            {
                await Context.Channel.SendMessageAsync("The host is still setting me up...");
                return;
            }

            if (song == "")
            {
                await Context.Channel.SendMessageAsync("You need to include a song name...\n`++ <songname>` aka `++ Amazed Lonestar`");
                return;
            }

            SearchResponse response = await SCommandHandler.FindSongAsync(song);
            if (response.Tracks.Total > 0 || response.Episodes.Total > 0)
            {
                List<SearchResult> results = new List<SearchResult>();
                string message = "";
                int id = 1;

                foreach (FullTrack track in response.Tracks.Items)
                {
                    if (id < 11)
                    {
                        string extra = "";
                        if (track.Explicit) extra = "**[EXPLICIT]**";
                        TimeSpan length = new TimeSpan(0, 0, 0, 0, track.DurationMs);
                        extra += " - " + length.Minutes + ":";
                        if (length.Seconds < 10) extra += "0" + length.Seconds;
                        else extra += length.Seconds;

                        message += $"\n**[{id,2} ]** {track.Name} by {track.Artists[0].Name} {extra}";
                        results.Add(new SearchResult() { Id = id.ToString(), Name = $"{track.Name} by {track.Artists}", Uri = track.Uri });
                        id++;
                    }
                }

                bool reqAdded = SCommandHandler.AddRequest(new SongRequest() { Results = results, UserId = Context.User.Id });
                if (reqAdded) await Context.Channel.SendMessageAsync("Which song do you want to queue?\n" + message);
            }
            else
            {
                await Context.Channel.SendMessageAsync("No tracks found :(");
                return;
            }
        }

        [Command("skipsong")]
        [Alias("-")]
        private async Task SkipSongAsync()
        {
            if (!SCommandHandler.Ready())
            {
                await Context.Channel.SendMessageAsync("The host is still setting me up...");
                return;
            }

            // todo: add voting
            await SCommandHandler.SkipSongAsync();
        }

        [Command("queue")]
        [Alias("=")]
        private async Task QueueAsync()
        {
            if (!SCommandHandler.Ready())
            {
                await Context.Channel.SendMessageAsync("The host is still setting me up...");
                return;
            }

            SCommandHandler.GetQueue();
        }

        [Command("help")]
        [Alias("?")]
        private async Task HelpAsync()
        {
            if (!SCommandHandler.Ready())
            {
                await Context.Channel.SendMessageAsync("The host is still setting me up...\nIf you need help with setup, refer to the GitHub readme.");
                return;
            }

            await Context.Channel.SendMessageAsync(
                $"Authorization token expires at {SCommandHandler.GetExpiry().ToShortTimeString()}\n\n" +
                $"Add a song to a playlist with: `++ <song>` for example `++ Amazed Lonestar`\n" +
                $"Skip the currently playing song with: `+-`\n" +
                $"Check the currently queued songs with: `+=`\n" +
                $"See this help with: `+?`\n\n"
            );
        }
    }
}

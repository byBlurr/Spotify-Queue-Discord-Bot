using Discord;
using Discord.Net.Bot;
using Discord.Net.Bot.Database.Configs;
using Discord.WebSocket;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace blurr.spotifybot
{
    public class SCommandHandler : CommandHandler
    {
        public override void RegisterCommands(List<BotCommand> commands)
        {
            commands.Clear();
        }

        public override void SetupHandlers(DiscordSocketClient bot)
        {
            bot.Ready += ReadyAsync;
            bot.MessageReceived += ProcessRequestsAsync;
        }

        private async Task ReadyAsync()
        {
            await Task.Delay(2500);
            await AuthorizeSpotifyAsync();
            UpdateStatus();
        }


        private static readonly string CLIENT_ID = "43082df32cdc4146b823a90d23ac34de";
        private static readonly string CLIENT_SECRET = "1a44b4c8649547ca9521c0da3be3775b";

        private static EmbedIOAuthServer Server;
        private static SpotifyClient SClient;
        private static DateTime SExpiry;
        private static string SToken, SRefresh;
        private static bool SReady = false;

        private static List<SongRequest> Requests = new List<SongRequest>();
        private static List<QueueItem> Queue = new List<QueueItem>();

        private async Task AuthorizeSpotifyAsync()
        {
            Server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await Server.Start();
            Server.AuthorizationCodeReceived += OnAuthorizationCodeReceivedAsync;

            var loginRequest = new LoginRequest(Server.BaseUri, CLIENT_ID, LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.UserModifyPlaybackState, Scopes.UserReadCurrentlyPlaying, Scopes.UserReadPlaybackPosition, Scopes.UserReadPlaybackState, Scopes.AppRemoteControl }
            };
            var uri = loginRequest.ToUri();

            BrowserUtil.Open(uri);
        }

        private async Task OnAuthorizationCodeReceivedAsync(object sender, AuthorizationCodeResponse response)
        {
            await Server.Stop();
            Server.Dispose();

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(new AuthorizationCodeTokenRequest(CLIENT_ID, CLIENT_SECRET, response.Code, new Uri("http://localhost:5000/callback")));

            SClient = new SpotifyClient(tokenResponse.AccessToken);
            SToken = tokenResponse.AccessToken;
            SRefresh = tokenResponse.RefreshToken;
            SReady = true;
            SExpiry = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn);

            Console.Clear();
            await Util.LoggerAsync(new LogMessage(LogSeverity.Info, "Spotify", "Spotify Connected. You can now start queueing songs."));
        }

        public void RenewToken(int seconds)
        {
            var renewThread = Task.Run(async () =>
            {
                await Task.Delay(1000 * (seconds - 120));
                SReady = false;
                await Util.LoggerAsync(new LogMessage(LogSeverity.Info, "Spotify", "Spotify token refreshing."));

                var config = SpotifyClientConfig.CreateDefault();
                var tokenResponse = await new OAuthClient(config).RequestToken(new AuthorizationCodeRefreshRequest(CLIENT_ID, CLIENT_SECRET, SRefresh));

                SClient = new SpotifyClient(tokenResponse.AccessToken);
                SToken = tokenResponse.AccessToken;
                SReady = true;
                SExpiry = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn);

                await Util.LoggerAsync(new LogMessage(LogSeverity.Info, "Spotify", "Spotify token refreshed."));
            });
        }

        public static bool Ready() => SReady;

        public static async Task AddSongToQueueAsync(string song)
        {
            PlayerAddToQueueRequest request = new PlayerAddToQueueRequest(song);
            await SClient.Player.AddToQueue(request);
        }

        public static async Task SkipSongAsync() => await SClient.Player.SkipNext();

        public static async Task<SearchResponse> FindSongAsync(string query)
        {
            SearchRequest request = new SearchRequest(SearchRequest.Types.Track, query);
            return await SClient.Search.Item(request);
        }

        public static List<QueueItem> GetQueue() => Queue;

        public static bool AddRequest(SongRequest request)
        {
            foreach (var r in Requests) if (r.UserId == request.UserId) return false;

            Requests.Add(request);
            return true;
        }

        public static DateTime GetExpiry() => SExpiry;

        private async Task ProcessRequestsAsync(SocketMessage msg)
        {
            if (msg == null) return;
            
            foreach (var r in Requests)
            {
                if (r.UserId == msg.Author.Id)
                {
                    string response = msg.Content;
                    if (response.Length == 1)
                    {
                        try
                        {
                            int song = int.Parse(response[0].ToString());
                            if (song > 0 && song <= r.Results.Count)
                            {
                                await AddSongToQueueAsync(r.Results[song - 1].Uri);
                                Requests.Remove(r);
                                Queue.Add(new QueueItem() { SongName = r.Results[song - 1].Name, SongUri = r.Results[song - 1].Uri, RequestedBy = msg.Author.Mention });
                                await msg.Channel.SendMessageAsync($"**{r.Results[song - 1].Name}** has been added to the queue!");
                            }
                            else await msg.Channel.SendMessageAsync("That wasn't a valid ID... what ID? THE NUMBER IN BOLD IN THE BOX. NEXT TO THE SONG YOU WANT");

                        }
                        catch
                        {
                            await msg.Channel.SendMessageAsync("That wasn't an ID... what ID? THE NUMBER IN BOLD IN THE BOX. NEXT TO THE SONG YOU WANT");
                        }
                    }
                    else
                    {
                        try
                        {
                            int song = int.Parse(response[0].ToString() + response[1].ToString());
                            if (song > 0 && song <= r.Results.Count)
                            {
                                await AddSongToQueueAsync(r.Results[song - 1].Uri);
                                Requests.Remove(r);
                                Queue.Add(new QueueItem() { SongName = r.Results[song - 1].Name, SongUri = r.Results[song - 1].Uri, RequestedBy = msg.Author.Mention });
                                await msg.Channel.SendMessageAsync($"**{r.Results[song - 1].Name}** has been added to the queue!");
                            }
                            else await msg.Channel.SendMessageAsync("That wasn't a valid ID... what ID? THE NUMBER IN BOLD IN THE BOX. NEXT TO THE SONG YOU WANT");

                        }
                        catch
                        {
                            await msg.Channel.SendMessageAsync("That wasn't an ID... what ID? THE NUMBER IN BOLD IN THE BOX. NEXT TO THE SONG YOU WANT");
                        }
                    }
                }
            }
        }

        public void UpdateStatus()
        {
            var update = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);

                    if (Ready())
                    {
                        PlayerCurrentlyPlayingRequest request = new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.All);
                        CurrentlyPlaying playing = await SClient.Player.GetCurrentlyPlaying(request);

                        if (playing != null)
                        {
                            if (playing.IsPlaying)
                            {
                                await bot.SetStatusAsync(UserStatus.Online);
                                string song = "";

                                switch (playing.Item.Type)
                                {
                                    case ItemType.Track:
                                        song = (playing.Item as FullTrack).Name;
                                        break;
                                    case ItemType.Episode:
                                        song = (playing.Item as FullEpisode).Name;
                                        break;
                                }

                                await bot.SetGameAsync(song, type: ActivityType.Listening);
                                if (Queue[0].SongName == song) Queue.RemoveAt(0);
                            }
                            else await bot.SetStatusAsync(UserStatus.DoNotDisturb);
                        }
                        else await bot.SetGameAsync($"Spotify", type: ActivityType.Listening);
                    }
                    else await bot.SetStatusAsync(UserStatus.DoNotDisturb);
                }
            });
        }
    }
}

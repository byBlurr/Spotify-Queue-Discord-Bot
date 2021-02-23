﻿using Discord;
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
        }

        private async Task ReadyAsync()
        {
            await Task.Delay(2500);
            await AuthorizeSpotifyAsync();
        }

        private static EmbedIOAuthServer Server;
        private static SpotifyClient SClient;
        private static string SToken, SRefresh;
        private static bool SReady = false;

        private async Task AuthorizeSpotifyAsync()
        {
            Server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await Server.Start();
            Server.AuthorizationCodeReceived += OnAuthorizationCodeReceivedAsync;

            var loginRequest = new LoginRequest(Server.BaseUri, "ClientId", LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.UserModifyPlaybackState, Scopes.UserReadCurrentlyPlaying, Scopes.UserReadPlaybackPosition, Scopes.UserReadPlaybackState, Scopes.AppRemoteControl }
            };
            var uri = loginRequest.ToUri();

            BrowserUtil.Open(uri);
        }

        private async Task OnAuthorizationCodeReceivedAsync(object sender, AuthorizationCodeResponse response)
        {
            await Server.Stop();

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(new AuthorizationCodeTokenRequest("ClientId", "ClientSecret", response.Code, new Uri("http://localhost:5000/callback")));

            SClient = new SpotifyClient(tokenResponse.AccessToken);
            SToken = tokenResponse.AccessToken;
            SRefresh = tokenResponse.RefreshToken;
            SReady = true;

            await Util.LoggerAsync(new LogMessage(LogSeverity.Info, "Spotify", "Spotify Connected. You can now start queueing songs."));
        }

        public static bool Ready() => SReady;

        public static async Task AddSongToQueueAsync(string song)
        {
            PlayerAddToQueueRequest request = new PlayerAddToQueueRequest(song);
            await SClient.Player.AddToQueue(request);
        }

        public static async Task SkipSongAsync() => await SClient.Player.SkipNext();

        public async Task UpdateStatusAsync()
        {
            var update = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);

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
                                    song = (playing.Item as FullTrack).Name + " by " + (playing.Item as FullTrack).Artists;
                                    break;
                                case ItemType.Episode:
                                    song = (playing.Item as FullEpisode).Name + " by " + (playing.Item as FullEpisode).Artists;
                                    break;
                            }

                            await bot.SetGameAsync(song, type: ActivityType.Listening);
                        }
                        else await bot.SetStatusAsync(UserStatus.DoNotDisturb);
                    }
                    else await bot.SetGameAsync($"Spotify", type: ActivityType.Listening);
                }
            });
        }
    }
}

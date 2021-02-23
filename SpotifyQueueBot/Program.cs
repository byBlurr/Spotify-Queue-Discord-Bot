using blurr.spotifybot;
using Discord.Net.Bot;
using System;

namespace SpotifyQueueBot
{
    class Program : Bot
    {
        static void Main(string[] args)
        {
            CommandHandler handler = new SCommandHandler();
            handler.ExecutableName = "SpotifyQueueBot";
            handler.RestartEveryMs = 21600000; // Every 6 hours
            StartBot(handler);
        }
    }
}

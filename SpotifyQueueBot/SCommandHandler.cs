using Discord;
using Discord.Net.Bot;
using Discord.Net.Bot.Database.Configs;
using Discord.WebSocket;
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
            await bot.SetStatusAsync(UserStatus.Online);
            await bot.SetGameAsync($"Spotify", type: ActivityType.Listening);


        }

        private void AuthorizeSpotify()
        {
        }
    }
}

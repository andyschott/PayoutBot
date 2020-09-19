using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PayoutBot.Discord;
using PayoutBot.Discord.Models;
using PayoutBot.Models;

namespace PayoutBot.Services
{
    public class RefreshPayouts : IHostedService, IDisposable
    {
        private readonly DiscordClient _discord;
        private readonly IOptions<RefreshConfig> _config;
        private Timer _timer = null;

        public RefreshPayouts(DiscordClient discord, IOptions<RefreshConfig> config)
        {
            _discord = discord;
            _config = config;
        }
        
        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var loginTask = _discord.Login();
            var playersTask = ParsePlayers(_config.Value.ShardDataPath);

            await Task.WhenAll(loginTask, playersTask);

            _timer = new Timer(async (state) => await UpdateDiscord((IEnumerable<Player>)state), playersTask.Result,
                TimeSpan.Zero, TimeSpan.FromMilliseconds(_config.Value.RefreshMilliseconds));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private Task UpdateDiscord(IEnumerable<Player> players)
        {
            return _discord.UpdatePayouts(players);
        }

        private static async Task<IEnumerable<Player>> ParsePlayers(string path)
        {
            using var stream = new FileStream(path, FileMode.Open);
            var players = await JsonSerializer.DeserializeAsync<IEnumerable<Player>>(stream);

            return players;
        }
    }
}
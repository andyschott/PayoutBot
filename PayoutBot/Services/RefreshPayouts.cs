using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PayoutBot.Discord;
using PayoutBot.Models;

namespace PayoutBot.Services
{
    public class RefreshPayouts : IHostedService, IDisposable
    {
        private readonly DiscordClient _discord;
        private readonly PayoutData _payoutData;
        private readonly IOptions<RefreshConfig> _config;
        private Timer _timer = null;

        public RefreshPayouts(DiscordClient discord, PayoutData payoutData,
            IOptions<RefreshConfig> config)
        {
            _discord = discord;
            _payoutData = payoutData;
            _config = config;
        }
        
        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _discord.Login();

            _timer = new Timer(async (state) => await UpdateDiscord(), null,
                TimeSpan.Zero, TimeSpan.FromMilliseconds(_config.Value.RefreshMilliseconds));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async Task UpdateDiscord()
        {
            var players = await _payoutData.GetData();
            await _discord.UpdatePayouts(players);
        }
    }
}
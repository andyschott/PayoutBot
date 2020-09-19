using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PayoutBot.Discord.Models;
using PayoutBot.Models;

namespace PayoutBot.Services
{
    public class PayoutData
    {
        private readonly Lazy<Task<IEnumerable<Player>>> _players;
        private readonly string _payoutDataPath;

        public PayoutData(IOptions<RefreshConfig> config)
        {
            _payoutDataPath = config.Value.ShardDataPath;
            _players = new Lazy<Task<IEnumerable<Player>>>(() => ParsePlayers(_payoutDataPath));
        }

        public Task<IEnumerable<Player>> GetData() => _players.Value;

        private static async Task<IEnumerable<Player>> ParsePlayers(string path)
        {
            using var stream = new FileStream(path, FileMode.Open);
            var players = await JsonSerializer.DeserializeAsync<IEnumerable<Player>>(stream);

            return players;
        }
    }
}
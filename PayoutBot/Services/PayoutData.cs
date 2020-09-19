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
    public class PayoutData : IDisposable
    {
        private Lazy<Task<IEnumerable<Player>>> _players;
        private readonly string _payoutDataPath;
        private FileSystemWatcher _watcher;

        public PayoutData(IOptions<RefreshConfig> config)
        {
            _payoutDataPath = config.Value.ShardDataPath;
            _players = InitPayoutData(_payoutDataPath);
            
            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_payoutDataPath),
                Filter = Path.GetFileName(_payoutDataPath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            _watcher.Changed += OnPayoutDataChanged;
            _watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            if(_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public Task<IEnumerable<Player>> GetData() => _players.Value;

        private void OnPayoutDataChanged(object sender, FileSystemEventArgs e)
        {
            _players = InitPayoutData(e.FullPath);
        }

        private static Lazy<Task<IEnumerable<Player>>> InitPayoutData(string path)
        {
            return new Lazy<Task<IEnumerable<Player>>>(() => ParsePlayers(path));
        }

        private static async Task<IEnumerable<Player>> ParsePlayers(string path)
        {
            using var stream = new FileStream(path, FileMode.Open);
            var players = await JsonSerializer.DeserializeAsync<IEnumerable<Player>>(stream);

            return players;
        }
    }
}
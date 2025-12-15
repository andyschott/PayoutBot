using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayoutBot.Discord.Models;
using PayoutBot.Models;

namespace PayoutBot.Services
{
    public class PayoutData : IDisposable
    {
        private Lazy<Task<IEnumerable<Player>>> _players;
        private readonly string _payoutDataPath;
        private readonly ILogger<PayoutData> _logger;
        private FileSystemWatcher _watcher;

        public PayoutData(IOptions<RefreshConfig> config,
          ILogger<PayoutData> logger)
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
            _logger = logger;
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

        private Lazy<Task<IEnumerable<Player>>> InitPayoutData(string path)
        {
            return new Lazy<Task<IEnumerable<Player>>>(() => ParsePlayers(path));
        }

        private async Task<IEnumerable<Player>> ParsePlayers(string path)
        {
            var loggingPath = Path.Combine(Directory.GetCurrentDirectory(),
              path);
            if (!File.Exists(path))
            {
                _logger.LogWarning("Payout data file not found at path: {Path}", loggingPath);
            }
            else
            {
                _logger.LogInformation("Loading payout data from path: {Path}", loggingPath);
            }
            using var stream = new FileStream(path, FileMode.Open);
            var players = await JsonSerializer.DeserializeAsync<IEnumerable<Player>>(stream);

            return players;
        }
    }
}
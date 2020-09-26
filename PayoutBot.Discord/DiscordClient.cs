using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayoutBot.Discord.Configuration;
using PayoutBot.Discord.Models;

namespace PayoutBot.Discord
{
    public class DiscordClient
    {
        private readonly DiscordSocketClient _client = new DiscordSocketClient();
        private readonly IOptions<BotConfig> _config;
        private readonly ILogger<DiscordClient> _logger;
        private IMessageChannel _writeChannel = null;
        private IUserMessage _message = null;

        private const string Thumbnail = "https://www.bobasalliance.com/wp-content/uploads/2020/01/bobasbot.png";

        public DiscordClient(IOptions<BotConfig> config, ILogger<DiscordClient> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task Login()
        {
            _logger.LogInformation("Logging in to Discord");
            await _client.LoginAsync(TokenType.Bot, _config.Value.Token);
            await _client.StartAsync();
            _client.Ready += OnReady;
        }

        public async Task UpdatePayouts(IEnumerable<Player> players)
        {
            _logger.LogInformation("Updating payouts");
            
            if(_message == null)
            {
                _logger.LogInformation("No message found in channel, returning");
                return;
            }

            var builder = new EmbedBuilder();
            builder.ThumbnailUrl = Thumbnail;
            builder.Description = "**Time until next payout**";
            builder.Footer = new EmbedFooterBuilder
            {
                Text = "Last Refresh",
                IconUrl = Thumbnail
            };
            builder.Timestamp = DateTimeOffset.UtcNow;

            var today = DateTime.UtcNow;
            var fields = players.Select(player =>
            {
                var payout = new DateTime(today.Year, today.Month, today.Day,
                    player.PayoutHour, 0, 0, DateTimeKind.Utc);
                if(payout < today)
                {
                    payout = payout.AddDays(1);
                }
                var timeUntilPayout = payout - today;
                return (
                    timeUntilPayout,
                    name: $"{timeUntilPayout.ToString(@"hh\:mm")} - (UTC {payout.ToString("HH:mm")})",
                    value: $"{player.Flag} [{player.Name}]({player.ProfileUrl})"
                );
            }).OrderBy(item => item.timeUntilPayout)
            .Select(item => new EmbedFieldBuilder
            {
                Name = item.name,
                Value = item.value
            });
            builder.Fields = fields.ToList();

            try
            {
                _logger.LogInformation("Updating message in Discord");
                await _message.ModifyAsync(props =>
                {
                    props.Embed = builder.Build();
                });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error updating message in Discord");
            }
        }

        private async Task OnReady()
        {
            _writeChannel = _client.GetChannel(_config.Value.WriteChannelId) as IMessageChannel;
            if(_writeChannel == null)
            {
                _logger.LogWarning("Could not find channel in Discord");
                return;
            }

            var messages = await GetMessages(_writeChannel);
            if(!messages.Any())
            {
                _logger.LogInformation("Creating new message for payouts");
                _message = await _writeChannel.SendMessageAsync(embed: new EmbedBuilder().Build());
            }
            else
            {
                _message = messages.First() as IUserMessage;
                if(_message.Embeds.Count == 0)
                {
                    _logger.LogInformation("Deleting message without any embeds and creating a new message for payouts");

                    await _message.DeleteAsync();
                    _message = await _writeChannel.SendMessageAsync(embed: new EmbedBuilder().Build());
                }
            }
        }

        private async Task<IEnumerable<IMessage>> GetMessages(IMessageChannel channel)
        {
            var messages = new List<IMessage>();
            var messageGroups = _writeChannel.GetMessagesAsync();
            await foreach(var group in messageGroups)
            {
                messages.AddRange(group);
            }

            return messages;
        }
    }
}

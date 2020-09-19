using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PayoutBot.Discord.Configuration
{
    public static class ServiceConfiguration
    {
        public static void AddDiscord(this IServiceCollection services, IConfiguration botConfigSection)
        {
            services.Configure<BotConfig>(botConfigSection);

            services.AddSingleton<DiscordClient>();
        }
    }
}
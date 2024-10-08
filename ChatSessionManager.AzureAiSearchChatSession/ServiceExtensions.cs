using ChatSessionManager.AzureAiSearchChatSession.Interfaces;
using ChatSessionManager.AzureAiSearchChatSession.Models.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatSessionManager.AzureAiSearchChatSession
{
    public static class ServiceExtensions
    {
        public static void AddAzureAISearchChatHistory(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            ArgumentNullException.ThrowIfNull(services, nameof(services));
            services.AddOptions();
            services.Configure<ChatSessionManagerOptions>(options => configuration.GetSection(nameof(ChatSessionManagerOptions)).Bind(options));
            services.AddScoped<ChatSessionManagerOptions>(); 
            services.AddKeyedScoped<IChatHistoryDataService, AzureAISearchChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
        }
    }
}

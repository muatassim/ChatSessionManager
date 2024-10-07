using ChatSessionManager.AzureAiSearchChatSession;
using ChatSessionManager.AzureAiSearchChatSessionTest.Configuration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel; 
namespace ChatSessionManager.AzureAiSearchChatSessionTest.Configuration
{
    public class AppHost
    {
        public static IConfiguration Configuration => GetServiceProvider().GetService<IConfiguration>();
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args) 
            .ConfigureAppConfiguration((context, config) =>
            {
                var appAssembly = typeof(AppHost).Assembly;
                config.AddUserSecrets(appAssembly, optional: true);
                // config.AddUserSecrets<Program>();
            })
            .ConfigureServices((context, services) =>
                {
                    services.AddLogging(logging =>
                    { 
                        logging.AddConsole();
                        logging.AddDebug(); 
                        logging.AddFile("Logs/log.txt", LogLevel.Information);

                    });
                    services.AddOptions();
                    services.Configure<AzureOpenAIOptions>(options => context.Configuration.GetSection(nameof(AzureOpenAIOptions)).Bind(options));
                    services.AddScoped<AzureOpenAIOptions>();

                    AzureOpenAIOptions azureOpenAIOptions =
                       context.Configuration.GetSection(nameof(AzureOpenAIOptions)).Get<AzureOpenAIOptions>();

                    // var azureOpenAIOptions = new AzureOpenAIOptions();
                    //  context.Configuration.GetSection(nameof(AzureOpenAIOptions)).Bind(azureOpenAIOptions);

                    services.AddAzureOpenAIChatCompletion(
                             deploymentName: azureOpenAIOptions.ModelName,
                             endpoint: azureOpenAIOptions.Endpoint,
                              apiKey: azureOpenAIOptions.Key);

                    services.AddAzureOpenAITextEmbeddingGeneration(
                        deploymentName: azureOpenAIOptions.EmbeddingModel,
                            endpoint: azureOpenAIOptions.Endpoint,
                             apiKey: azureOpenAIOptions.Key);
                    services.AddTransient((serviceProvider) =>
                    {
                        return new Kernel(serviceProvider);
                    });
                    services.AddAzureAISearchChatHistory(context.Configuration);
                });
        private static IHost _host;

        public static async Task RunAsync(string[] args)
        {
            IHost host = _host ?? CreateHostBuilder(args).Build();
            _host ??= host;
            await host.RunAsync();
            // var myService = host.Services.GetRequiredService<MyService>();
            //await myService.RunAsync();
        }
        public static IServiceProvider GetServiceProvider()
        {
            _host ??= CreateHostBuilder([]).Build();
            return _host?.Services ?? CreateHostBuilder([]).Build().Services;
        }


        public static string LogPath => Path.Combine(Environment.CurrentDirectory, "Logs");
        public static bool OpenLogFolder
        {
            get
            {
                try
                {
                    if (bool.TryParse(Configuration.GetSection("OpenLogFolder").Get<string>(), out bool open))
                    {
                        return open;
                    }
                }
                catch
                {
                    return false;
                }
                return false;
            }
        }
    }



}

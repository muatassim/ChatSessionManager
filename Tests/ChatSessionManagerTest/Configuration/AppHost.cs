using ChatSessionManager;
using ChatSessionManagerTest.Configuration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Reflection;
namespace ChatSessionManagerTest.Configuration
{
    public class AppHost
    {
        public static IConfiguration Configuration => GetServiceProvider().GetService<IConfiguration>();
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                //logging.AddConsole();
                //logging.AddDebug();
                try
                {
                    string fileName = $"{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}";
                    string logFilePath = "Logs/" + Assembly.GetExecutingAssembly().GetName().Name + "-" + fileName + ".txt";
                    logging.AddFile(logFilePath, LogLevel.Information, isJson: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to configure file logging: {ex.Message}");
                }
            })
            .ConfigureAppConfiguration((context, config) =>
            {
                var appAssembly = typeof(AppHost).Assembly;
                config.AddUserSecrets(appAssembly, optional: true);
            })
            .ConfigureServices((context, services) =>
                {
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
                    services.AddCosmosChatHistory(context.Configuration);
                });
        private static IHost _host;

        public static async Task RunAsync(string[] args)
        {
            IHost host = _host ?? CreateHostBuilder(args).Build();
            _host ??= host;

            var logger = host.Services.GetRequiredService<ILogger<AppHost>>();
            logger.LogInformation("Application starting up...");
            ILoggerFactory loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

            await host.RunAsync();
        }
        public static IServiceProvider GetServiceProvider()
        {
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

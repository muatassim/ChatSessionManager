using ChatSessionManager.AzureAiSearchChatSession; 
using ChatSessionManager.AzureAiSearchChatSessionTest.Configuration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel; 
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace ChatSessionManager.AzureAiSearchChatSessionTest.Configuration
{
    public class AppHost
    {
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => {
                   var appAssembly = typeof(AppHost).Assembly;
                     config.AddUserSecrets(appAssembly, optional: true);
                   // config.AddUserSecrets<Program>();
                 })
                .ConfigureServices((context, services) =>
                {
                  
                    services.AddOptions();
                    services.Configure<AzureOpenAIOptions>(options => context.Configuration.GetSection(nameof(AzureOpenAIOptions)).Bind(options));
                    services.AddScoped<AzureOpenAIOptions>();

                   // AzureOpenAIOptions azureOpenAIOptions =
                   //    context.Configuration.GetSection(nameof(AzureOpenAIOptions)).Get<AzureOpenAIOptions>();

                    var azureOpenAIOptions = new AzureOpenAIOptions();
                    context.Configuration.GetSection(nameof(AzureOpenAIOptions)).Bind(azureOpenAIOptions);

                    services.AddAzureOpenAIChatCompletion(
                             deploymentName: azureOpenAIOptions.ModelName,
                             endpoint: azureOpenAIOptions.Endpoint,
                              apiKey: azureOpenAIOptions.Key);

                    services.AddAzureOpenAITextEmbeddingGeneration(
                        deploymentName: azureOpenAIOptions.EmbeddingModel,
                            endpoint: azureOpenAIOptions.Endpoint,
                             apiKey: azureOpenAIOptions.Key);

                    // Register IChatCompletionService as a singleton
                    //services.AddSingleton<IChatCompletionService>(provider =>
                    //     new AzureOpenAIChatCompletionService(
                    //         deploymentName: azureOpenAIOptions.ModelName,
                    //         endpoint: azureOpenAIOptions.Endpoint,
                    //          apiKey: azureOpenAIOptions.Key));

                    //services.AddSingleton<ITextEmbeddingGenerationService>(provider =>
                    //    new AzureOpenAITextEmbeddingGenerationService(
                    //        deploymentName: azureOpenAIOptions.EmbeddingModel,
                    //        endpoint: azureOpenAIOptions.Endpoint,
                    //         apiKey: azureOpenAIOptions.Key));

                    services.AddTransient((serviceProvider) =>
                    {
                        return new Kernel(serviceProvider);
                    });
                    // services.Add<ApplicationService>(); 
                    services.AddAzureAiSearchChatHistory(context.Configuration);
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
    }


    public class KernelHelper
    {
        public void Get()
        {
            Kernel kernel = new(AppHost.GetServiceProvider());
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.Build();
        }
    }
}

using ChatSessionManager.AzureAiSearchChatSession;
using ChatSessionManager.AzureAiSearchChatSession.Interfaces; 
using ChatSessionManager.AzureAiSearchChatSessionTest.Configuration;
using ChatSessionManager.AzureAiSearchChatSessionTest.Configuration.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Serilog;
namespace ChatSessionManager.AzureAiSearchChatSessionTest
{
    [TestClass]
    public class AzureAiSearchChatHistoryDataServiceTest 
    {
        static readonly bool skipDataSourceDeletionTest = true;
        [TestMethod]
        public void AzureAiSearchChatHistoryDataServiceTestIsNotNull()
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService); 
        }

        [TestMethod]
        public async Task CreateDataSourceIfNotExistAsyncTest()
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            (List<AzureAiSearchChatSession.Models.LogMessage> messages, bool success) =
                await chatHistoryDataService.CreateDataSourceIfNotExistAsync(); 
            Assert.IsNotNull(messages);
            Assert.IsTrue(success);
        }

        [TestMethod]
        public async Task DeleteIfDataSourceExistsAsyncTest()
        {
            if (skipDataSourceDeletionTest)
                return;
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            (List<AzureAiSearchChatSession.Models.LogMessage> messages, bool success) = 
                await chatHistoryDataService.DeleteIfDataSourceExistsAsync(); 
            Assert.IsNotNull(messages);
            Assert.IsTrue(success);
        }


        [TestMethod]
        [DataRow("What is the capital of Pakistan")]
        public async Task AddDocumentAsyncTest(string question)
        {
            Log.Information("This is a log message from TestMethod1");
            IOptions <AzureOpenAIOptions> options = AppHost.GetServiceProvider().GetRequiredService<IOptions<AzureOpenAIOptions>>();
            Assert.IsNotNull(options);
            //var openAI = new OpenAI(options.Value.Key, options.Value.Endpoint);
            AzureOpenAIOptions azureOpenAIOptions = options.Value as AzureOpenAIOptions;

           
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

            Assert.IsNotNull(kernel);

            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            Assert.IsNotNull(chatCompletionService);


          
            ITextEmbeddingGenerationService textEmbeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();  

            Assert.IsNotNull(textEmbeddingGenerationService);

            ReadOnlyMemory<float> questionEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(question);
            
            Assert.IsNotNull(questionEmbedding);
            //Create ChatDocument 


            //Send the question to Azure Open Ai 


           ChatMessageContent chatMessageContent = await  chatCompletionService.GetChatMessageContentAsync(question);
           
          

        }




    }
}
using ChatSessionManager.AzureAiSearchChatSession;
using ChatSessionManager.AzureAiSearchChatSession.Interfaces;
using ChatSessionManager.AzureAiSearchChatSession.Models;
using ChatSessionManager.AzureAiSearchChatSessionTest.Configuration;
using ChatSessionManager.AzureAiSearchChatSessionTest.Configuration.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings; 
namespace ChatSessionManager.AzureAiSearchChatSessionTest
{
    [TestClass]
    public class AzureAiSearchChatHistoryDataServiceTest 
    {
        static readonly bool skipDataSourceDeletionTest = true;
        static readonly string userId = "B85A4454-A007-449C-B1DA-0136BFE6248B";
        static readonly string sessionId = Guid.NewGuid().ToString();
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
        public async Task GetRelatedRecordsTestAsync(string question)
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

            Assert.IsNotNull(kernel);
            ITextEmbeddingGenerationService textEmbeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            Assert.IsNotNull(textEmbeddingGenerationService);
            //Get Question 1 Vector 
            ReadOnlyMemory<float> questionEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(question);
            Assert.IsNotNull(questionEmbedding);
            List<ChatDocument> historyRecords = await chatHistoryDataService.GetDocumentsByQueryAsync(question, questionEmbedding, 2);
            Assert.IsNotNull(historyRecords);



        }


        [TestMethod]
        [DataRow("What is the capital of Pakistan", "What is the population of that capital?")]
        public async Task AddDocumentAsyncTest(string question1, string question2)
        {

            IOptions<AzureOpenAIOptions> options = AppHost.GetServiceProvider().GetRequiredService<IOptions<AzureOpenAIOptions>>();
            Assert.IsNotNull(options);
            //var openAI = new OpenAI(options.Value.Key, options.Value.Endpoint);
            AzureOpenAIOptions azureOpenAIOptions = options.Value as AzureOpenAIOptions;

            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);

            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

            Assert.IsNotNull(kernel);

            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            Assert.IsNotNull(chatCompletionService); 
            ITextEmbeddingGenerationService textEmbeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            Assert.IsNotNull(textEmbeddingGenerationService);
            //Get Question 1 Vector 
            ReadOnlyMemory<float> question1Embedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(question1);
            Assert.IsNotNull(question1Embedding);

            await AskQuestion(question1, chatHistoryDataService, chatCompletionService,question1Embedding);


            //Second Question Vector 
            ReadOnlyMemory<float> question2Embedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(question2);
            Assert.IsNotNull(question2Embedding);


            //Start next Question 
            await AskQuestion(question2, chatHistoryDataService, chatCompletionService, question2Embedding);
            //Get History Records 


            //Send the Question Again 

        }

        private static async Task AskQuestion(string question, 
            IChatHistoryDataService chatHistoryDataService,
            IChatCompletionService chatCompletionService,
            ReadOnlyMemory<float> questionEmbedding )
        {
            ///Question 1 Record 
            ChatHistory chatHistory = [];
            chatHistory.AddSystemMessage("You are an AI assistant who answers the users questions in a thoughtfull manner and are precise with your answer.");
            List<ChatDocument> historyRecords = await chatHistoryDataService.GetDocumentsByQueryAsync(question, questionEmbedding, 2);
            if (historyRecords != null)
            {
                //Add history Records 
                foreach (ChatDocument document in historyRecords)
                {
                    // Add user messages and AI responses based on their roles
                      chatHistory.AddUserMessage(document.Question);
                      chatHistory.AddAssistantMessage(document.Content);
                }
            }
           
            //Add the last user message with is the question 
            chatHistory.AddUserMessage(question);

            ChatMessageContent messageContent = await chatCompletionService.GetChatMessageContentAsync(question);
            //Save Question 1 and Response 
            await SaveChat(question, chatHistoryDataService, questionEmbedding, chatHistory, messageContent);
        }

        private static async Task SaveChat(string question1, IChatHistoryDataService chatHistoryDataService, ReadOnlyMemory<float> questionEmbedding, ChatHistory chatHistory, ChatMessageContent messageContent)
        {
            ChatDocument chatDocument = new()
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Content = messageContent.Content,
                IpAddress = "127.0.0.1",
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                QuestionVector = questionEmbedding,
                Question = question1,
                Role = AuthorRole.User.Label
            };
            chatHistory.Add(messageContent);
            //Save the conversation to the UserStore 
            (List<LogMessage> messages, bool success) response = await chatHistoryDataService.AddDocumentAsync(chatDocument);
            Assert.IsNotNull(response);
        }



    }
}
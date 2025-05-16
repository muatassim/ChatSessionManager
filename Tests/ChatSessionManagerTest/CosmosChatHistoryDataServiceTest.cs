using ChatSessionManager;
using ChatSessionManager.Interfaces;
using ChatSessionManager.Models;
using ChatSessionManagerTest.Configuration;
using ChatSessionManagerTest.Configuration.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using System.Linq.Expressions;
using Microsoft.Extensions.AI;

namespace ChatSessionManagerTest
{
    [TestClass]
    public class CosmosChatHistoryDataServiceTest
    {
        static readonly bool skipDataSourceDeletionTest = true;
        static readonly string userId = "B85A4454-A007-449C-B1DA-0136BFE6248B";
        static readonly string sessionId = Guid.NewGuid().ToString();


        [TestInitialize]
        public async Task InitializeTestDataAsync()
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);

            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Assert.IsNotNull(kernel);

           var textEmbeddingGenerationService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            Assert.IsNotNull(textEmbeddingGenerationService);

            var sampleQuestions = new List<string>
            {
                "What is the capital of France?",
                "How does machine learning work?",
                "Tell me a fun fact about space."
            };

            foreach (var question in sampleQuestions)
            {
               var questionEmbedding = await textEmbeddingGenerationService.GenerateAsync(question);
                Assert.IsNotNull(questionEmbedding);

                ChatDocument chatDocument = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Content = $"Sample response for '{question}'",
                    IpAddress = "127.0.0.1",
                    SessionId = sessionId,
                    Timestamp = DateTime.UtcNow,
                    QuestionVector = questionEmbedding.Vector,
                    Question = question,
                    Role = AuthorRole.User.Label
                };

                (_, bool success) = await chatHistoryDataService.AddDocumentAsync(chatDocument);
                Assert.IsTrue(success);
            }
        }


        [TestMethod]
        public void CosmosChatHistoryDataServiceTestIsNotNull()
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
        }

        [TestMethod]
        public async Task CreateDataSourceIfNotExistAsyncTest()
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            (List<LogMessage> messages, bool success) =
                await chatHistoryDataService.CreateDataSourceIfNotExistAsync();
            Assert.IsNotNull(messages);
            Assert.IsTrue(success);
        }

        [TestMethod]
        public async Task DeleteIfDataSourceExistsAsyncTest()
        {
            if (skipDataSourceDeletionTest)
                return;
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            (List<LogMessage> messages, bool success) =
                await chatHistoryDataService.DeleteIfDataSourceExistsAsync();
            Assert.IsNotNull(messages);
            Assert.IsTrue(success);
        }
        [DataTestMethod]
        [DataRow("What is the capital of Pakistan")]
        [DataRow("what is the capital of India")]
        public async Task AddDocumentsAsyncTest(string question)
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Assert.IsNotNull(kernel);
           var textEmbeddingGenerationService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            Assert.IsNotNull(textEmbeddingGenerationService);
            //Get Question 1 Vector 
            var questionEmbedding = await textEmbeddingGenerationService.GenerateAsync(question);
            Assert.IsNotNull(questionEmbedding);

            ChatDocument chatDocument = new()
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Content = "This is a chat message",
                IpAddress = "192.168.1.1",
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                Role = "user",
                QuestionVector = questionEmbedding.Vector,
                Question = question
            };

            (List<LogMessage> messages, bool success) = await chatHistoryDataService.AddDocumentAsync(chatDocument);
            Assert.IsTrue(success);

        }

        [TestMethod]
        [DataRow("What is the capital of Pakistan", 2)]
        public async Task GetRelatedRecordsTestAsync(string question, int size)
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

            Assert.IsNotNull(kernel);
           var textEmbeddingGenerationService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            Assert.IsNotNull(textEmbeddingGenerationService);
            //Get Question 1 Vector 
            var questionEmbedding = await textEmbeddingGenerationService.GenerateAsync(question);
            Assert.IsNotNull(questionEmbedding);
            List<ChatDocument> historyRecords = await chatHistoryDataService.GetDocumentsByQueryAsync(question, questionEmbedding.Vector, size, userId);
            Assert.IsNotNull(historyRecords);
        }





        [TestMethod]
        [DataRow("B85A4454-A007-449C-B1DA-0136BFE6248B")]
        public async Task GetDocumentsByUserIdAsync_Test(string userId)
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();


            List<ChatDocument> historyRecords = await chatHistoryDataService.GetDocumentsByUserIdAsync(userId);
            Assert.IsNotNull(historyRecords);
        }
        [TestMethod]
        [DataRow("B85A4454-A007-449C-B1DA-0136BFE6248B")]
        public async Task GetDocumentFindAsync_Test(string userId)
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Expression<Func<ChatDocument, bool>> expr = x => x.UserId == userId;
            ChatDocument records = await chatHistoryDataService.FindAsync(expr);
            Assert.IsNotNull(records);
        }

        [DataTestMethod]
        [DataRow("What is the capital of Pakistan")]
        public async Task GetDocumentByQueryFindAsync_Test(string question)
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Expression<Func<ChatDocument, bool>> expr = x => x.Question == question;
            ChatDocument records = await chatHistoryDataService.FindAsync(expr);
            Assert.IsNotNull(records);
        }

        [TestMethod] 
        public async Task GetDocumentFindAllAsync_Test()
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

            Expression<Func<ChatDocument, bool>> expr = x => x.UserId == userId;
            List<ChatDocument> records = await chatHistoryDataService.FindAllAsync(expr);
            Assert.IsNotNull(records);
        }


        [TestMethod] 
        public async Task GetHistoryContextByUserId ()
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

            Expression<Func<ChatDocument, bool>> expr = x => x.UserId == userId;
            HistoryContext context = await chatHistoryDataService.GetChatHistoryContextAsync(expr);
            Assert.IsNotNull(context);
        }

        [TestMethod]
        public async Task FindDocumentsByUserIdAndSessionIdAsync_Test()
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Expression<Func<ChatDocument, bool>> expr = x => x.UserId == userId && x.SessionId == sessionId;
            ChatDocument records = await chatHistoryDataService.FindAsync(expr);
            Assert.IsNotNull(records);
        }

        [TestMethod]
        [DataRow("I'm planning a trip to Paris. Can you tell me the best time of year to visit and some must-see attractions?",
            "Given that I'm interested in art and history, what are some lesser-known museums in Paris that I should visit?")]
        [DataRow("I want to adopt a healthier diet. Can you suggest some nutritious foods to incorporate into my meals?",
           "Based on the foods you suggested, can you give me a simple recipe for a balanced meal?")]
        [DataRow("I'm looking for a good mystery novel to read. Can you suggest one?", "Sounds interesting. What can you tell me about the main character in 'The Girl with the Dragon Tattoo'?")]
        [DataRow("I want to start a workout routine to build muscle. Any tips on what exercises I should do?", "Can you suggest a weekly workout plan that includes those exercises?")]
        public async Task ChatWithHistoryExampleAsync_Test(string question, string followUpQuestion)
        {

            IOptions<AzureOpenAIOptions> options = AppHost.GetServiceProvider().GetRequiredService<IOptions<AzureOpenAIOptions>>();
            Assert.IsNotNull(options);
            //var openAI = new OpenAI(options.Value.Key, options.Value.Endpoint);
            AzureOpenAIOptions azureOpenAIOptions = options.Value as AzureOpenAIOptions;

            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(CosmosChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);

            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

            Assert.IsNotNull(kernel);

            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            Assert.IsNotNull(chatCompletionService);
           var textEmbeddingGenerationService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            Assert.IsNotNull(textEmbeddingGenerationService);
            //Get Question 1 Vector 
            var questionEmbedding = await textEmbeddingGenerationService.GenerateAsync(question);
            Assert.IsNotNull(questionEmbedding);

            await AskQuestion(question, chatHistoryDataService, chatCompletionService, questionEmbedding.Vector);


            //Second Question Vector 
            var followUpQuestionEmbedding = await textEmbeddingGenerationService.GenerateAsync(followUpQuestion);
            Assert.IsNotNull(followUpQuestionEmbedding);


            //Start next Question 
            await AskQuestion(followUpQuestion, chatHistoryDataService, chatCompletionService, followUpQuestionEmbedding.Vector);
            //Get History Records 


            //Send the Question Again 

        }

        private static async Task AskQuestion(string question,
            IChatHistoryDataService chatHistoryDataService,
            IChatCompletionService chatCompletionService,
            ReadOnlyMemory<float> questionEmbedding)
        {
            ///Question 1 Record 
            ChatHistory chatHistory = [];
            chatHistory.AddSystemMessage("You are an AI assistant who answers the users questions in a thought full manner and are precise with your answer.");
            //Add history and usermessage 
            var historyContext = await chatHistoryDataService.GetChatHistoryContextAsync(question, questionEmbedding, 2, userId, 0.5);
            if (historyContext != null)
            {
                chatHistory.AddMessage(AuthorRole.Assistant, historyContext.ToString());
            }
            chatHistory.AddUserMessage(question);

            ChatMessageContent messageContent = await chatCompletionService.GetChatMessageContentAsync(question);
            //Save Question 1 and Response 
            // await SaveChat(question, chatHistoryDataService, questionEmbedding, chatHistory, messageContent);
        }
        private static async Task SaveChat(string question, IChatHistoryDataService chatHistoryDataService, ReadOnlyMemory<float> questionEmbedding, ChatHistory chatHistory, ChatMessageContent messageContent)
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
                Question = question,
                Role = AuthorRole.User.Label
            };
            chatHistory.Add(messageContent);
            //Save the conversation to the UserStore 
            var (_, success) = await chatHistoryDataService.AddDocumentAsync(chatDocument);
            Assert.IsTrue(success);
        }



    }
}
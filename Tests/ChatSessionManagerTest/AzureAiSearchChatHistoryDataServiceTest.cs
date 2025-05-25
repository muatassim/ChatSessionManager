using ChatSessionManager;
using ChatSessionManager.Interfaces;
using ChatSessionManager.Models;
using ChatSessionManagerTest.Configuration;
using ChatSessionManagerTest.Configuration.Model;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Layouts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using System.Linq.Expressions;

namespace ChatSessionManagerTest
{
    [TestClass]
    [DoNotParallelize]
    public class AzureAiSearchChatHistoryDataServiceTest
    {
        static readonly bool skipDataSourceDeletionTest = true;
        [TestMethod]
        public void AzureAiSearchChatHistoryDataServiceTestIsNotNull()
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService); 
        }


        [TestInitialize]
        public async Task InitializeTestDataAsync()
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);

            // Ensure index exists
            await chatHistoryDataService.CreateDataSourceIfNotExistAsync();


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

            string userId = "B85A4454-A007-449C-B1DA-0136BFE6248B";
            string sessionId = Guid.NewGuid().ToString();
            foreach (var question in sampleQuestions)
            {
                Embedding<float> questionEmbedding = await textEmbeddingGenerationService.GenerateAsync(question);
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
        public async Task CreateDataSourceIfNotExistAsyncTest()
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
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
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            (List<LogMessage> messages, bool success) =
                await chatHistoryDataService.DeleteIfDataSourceExistsAsync();
            Assert.IsNotNull(messages);
            Assert.IsTrue(success);
        }
        [TestMethod]
        [DataRow("What is the capital of Pakistan")]
        public async Task AddDocumentsAsyncTest(string question)
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
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
                UserId = Guid.NewGuid().ToString(),
                Content = "This is the response from Ai",
                IpAddress = "127.0.0.1",
                SessionId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                QuestionVector = questionEmbedding.Vector,
                Question = question,
                Role = AuthorRole.User.Label
            };

            (_, bool success) = await chatHistoryDataService.AddDocumentAsync(chatDocument);
            Assert.IsTrue(success);

        }


        [TestMethod]
        [DataRow("What is the capital of Pakistan", 2, 0.4)]
        public async Task GetRelatedRecordsTestAsync(string question, int size, double rerankerScore)
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Assert.IsNotNull(kernel);
            var textEmbeddingGenerationService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            Assert.IsNotNull(textEmbeddingGenerationService);
            //Get Question 1 Vector 
            var questionEmbedding = await textEmbeddingGenerationService.GenerateAsync(question);
            Assert.IsNotNull(questionEmbedding);
            List<ChatDocument> historyRecords = await chatHistoryDataService.GetDocumentsByQueryAsync(question, questionEmbedding.Vector, size, "B85A4454-A007-449C-B1DA-0136BFE6248B", rerankerScore);
            Assert.IsNotNull(historyRecords);
        }





        [TestMethod] 
        public async Task GetDocumentsByUserIdAsync_Test()
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();


            List<ChatDocument> historyRecords = await chatHistoryDataService.GetDocumentsByUserIdAsync("B85A4454-A007-449C-B1DA-0136BFE6248B");
            Assert.IsNotNull(historyRecords);
        }

        [TestMethod]
        public async Task GetDocumentsByUserIdAndSessionIdAsync_Test()
        {
            string localSessionId = Guid.NewGuid().ToString();
            //string question,string content,string localUserId, string localSessionId, string id
            IChatHistoryDataService chatHistoryDataService = await AddAndGetChatHistoryDataService("myQuestion",
                "my content",
                "B85A4454-A007-449C-B1DA-0136BFE6248B",
                localSessionId: localSessionId,
                id: Guid.NewGuid().ToString()); 
            List<ChatDocument> historyRecords = await chatHistoryDataService.GetDocumentsByUserIdAndSessionIdAsync("B85A4454-A007-449C-B1DA-0136BFE6248B", localSessionId);
            Assert.IsNotNull(historyRecords);
        }
        [TestMethod] 
        public async Task GetDocumentFindAsync_Test()
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

         
            Expression<Func<ChatDocument, bool>> expr = x => x.UserId == "B85A4454-A007-449C-B1DA-0136BFE6248B";
            ChatDocument records = await chatHistoryDataService.FindAsync(expr);
            Assert.IsNotNull(records);
        }
        [TestMethod]
        public async Task FindDocumentsByUserIdAndSessionIdAsync_Test()
        {
            string localSessionId =Guid.NewGuid().ToString();
            //string question,string content,string localUserId, string localSessionId, string id
            IChatHistoryDataService chatHistoryDataService = await AddAndGetChatHistoryDataService("myQuestion",
                "my content",
                "B85A4454-A007-449C-B1DA-0136BFE6248B",
                localSessionId: localSessionId,
                id: Guid.NewGuid().ToString());
            Expression<Func<ChatDocument, bool>> expr = x => x.UserId == "B85A4454-A007-449C-B1DA-0136BFE6248B" && x.SessionId == localSessionId; 
            ChatDocument records = await chatHistoryDataService.FindAsync(expr);
            Assert.IsNotNull(records);
        }

        [TestMethod]
        public async Task FindAllDocumentsByUserIdAndSessionIdAsync_Test()
        {
            string localSessionId = Guid.NewGuid().ToString();
            //string question,string content,string localUserId, string localSessionId, string id
            IChatHistoryDataService chatHistoryDataService = await AddAndGetChatHistoryDataService("myQuestion",
                "my content",
                "B85A4454-A007-449C-B1DA-0136BFE6248B",
                localSessionId: localSessionId,
                id: Guid.NewGuid().ToString());
            Expression<Func<ChatDocument, bool>> expr = x => x.UserId == "B85A4454-A007-449C-B1DA-0136BFE6248B" && x.SessionId == localSessionId;
            List<ChatDocument> records = await chatHistoryDataService.FindAllAsync(expr);
            Assert.IsNotNull(records);
        }
        [DataTestMethod]
        [DataRow("I'm planning a trip to Paris. Can you tell me the best time of year to visit and some must-see attractions?")]
        public async Task GetDocumentByQueryFindAsync_Test(string question)
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Expression<Func<ChatDocument, bool>> expr = x => x.Question == question;
            ChatDocument records = await chatHistoryDataService.FindAsync(expr);
            Assert.IsNotNull(records);
        }

        [TestMethod] 
        public async Task GetDocumentFindAllAsync_Test()
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

            Expression<Func<ChatDocument, bool>> expr = x => x.UserId == "B85A4454-A007-449C-B1DA-0136BFE6248B";
            List<ChatDocument> records = await chatHistoryDataService.FindAllAsync(expr);
            Assert.IsNotNull(records);
        }


        [TestMethod] 
        public async Task GetHistoryContextByUserId()
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

            Expression<Func<ChatDocument, bool>> expr = x => x.UserId == "B85A4454-A007-449C-B1DA-0136BFE6248B";
            HistoryContext context = await chatHistoryDataService.GetChatHistoryContextAsync(expr);
            Assert.IsNotNull(context);
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

            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
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
            // Question 1 Record 
            ChatHistory chatHistory = [];
            chatHistory.AddSystemMessage("You are an AI assistant who answers the users questions in a thought full manner and are precise with your answer.");
            //Add history and userMessage 
            var historyContext = await chatHistoryDataService.GetChatHistoryContextAsync(question, questionEmbedding, 2, "B85A4454-A007-449C-B1DA-0136BFE6248B", 0.5);
            if (historyContext != null)
            {
                chatHistory.AddMessage(AuthorRole.Assistant, historyContext.ToString());
            }
            chatHistory.AddUserMessage(question);

            ChatMessageContent messageContent = await chatCompletionService.GetChatMessageContentAsync(question);
            //Save Question 1 and Response 
            await SaveChat(question, chatHistoryDataService, questionEmbedding, chatHistory, messageContent);
        }
        private static async Task SaveChat(string question, IChatHistoryDataService chatHistoryDataService, ReadOnlyMemory<float> questionEmbedding, ChatHistory chatHistory, ChatMessageContent messageContent)
        {
            string localSessionId = Guid.NewGuid().ToString();
            ChatDocument chatDocument = new()
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "B85A4454-A007-449C-B1DA-0136BFE6248B",
                Content = messageContent.Content,
                IpAddress = "127.0.0.1",
                SessionId = localSessionId,
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

        [TestMethod]
        [DataRow("Test question for delete by id","Deleting question by Id")]
        public async Task DeleteDocumentAsync_Test(string question, string content )
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService); 
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Assert.IsNotNull(kernel);
            var textEmbeddingGenerationService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            Assert.IsNotNull(textEmbeddingGenerationService);
            //Get Question 1 Vector 
            var questionEmbedding = await textEmbeddingGenerationService.GenerateAsync(question);
            Assert.IsNotNull(questionEmbedding);

            string id = Guid.NewGuid().ToString();
            string userIdNew = Guid.NewGuid().ToString();
            string sessionIdNew = Guid.NewGuid().ToString();
            // Add a document to delete
            var chatDocument = new ChatDocument
            {
                Id = id,
                UserId = userIdNew,
                Content = content,
                IpAddress = "127.0.0.1",
                SessionId = sessionIdNew,
                Timestamp = DateTime.UtcNow,
                QuestionVector =questionEmbedding.Vector,
                Question = question,
                Role = AuthorRole.User.Label
            };
            (_, bool addSuccess) = await chatHistoryDataService.AddDocumentAsync(chatDocument);
            Assert.IsTrue(addSuccess);

            // Delete the document
            bool deleteSuccess = await chatHistoryDataService.DeleteDocumentAsync(chatDocument.Id);
            Assert.IsTrue(deleteSuccess);

            // Verify deletion
            var deleted = await chatHistoryDataService.FindAsync(x => x.Id == chatDocument.Id);
            Assert.IsNull(deleted);
        }

        [TestMethod]
        [DataRow("Test question for delete by id", "Deleting question by Id")]
        public async Task DeleteDocumentByUserIdAsync_Test(string question, string content)
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Assert.IsNotNull(kernel);
            var textEmbeddingGenerationService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            Assert.IsNotNull(textEmbeddingGenerationService);
            //Get Question 1 Vector 
            Embedding<float> questionEmbedding = await textEmbeddingGenerationService.GenerateAsync(question);
            Assert.IsNotNull(questionEmbedding);

            string id = Guid.NewGuid().ToString();
            string userIdNew = Guid.NewGuid().ToString();
            string sessionIdNew = Guid.NewGuid().ToString();
            // Add a document to delete
            var chatDocument = new ChatDocument
            {
                Id = id,
                UserId = userIdNew,
                Content = content,
                IpAddress = "127.0.0.1",
                SessionId = sessionIdNew,
                Timestamp = DateTime.UtcNow,
                QuestionVector = questionEmbedding.Vector,
                Question = question,
                Role = AuthorRole.User.Label
            };
            (_, bool addSuccess) = await chatHistoryDataService.AddDocumentAsync(chatDocument);
            Assert.IsTrue(addSuccess);

            Thread.Sleep(2000);
            // Delete by userId
            bool deleteSuccess = await chatHistoryDataService.DeleteDocumentByUserIdAsync(userIdNew);
            Assert.IsTrue(deleteSuccess);

            // Verify deletion
            var deleted = await chatHistoryDataService.FindAsync(x => x.UserId == userIdNew);
            Assert.IsNull(deleted);
        }

        [TestMethod]
        [DataRow("Test question for delete by id", "Deleting question by Id")]
        public async Task DeleteDocumentByUserIdAndSessionIdAsync_Test(string question, string content)
        {
            IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
            Assert.IsNotNull(chatHistoryDataService);
            Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();
            Assert.IsNotNull(kernel);
            var textEmbeddingGenerationService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            Assert.IsNotNull(textEmbeddingGenerationService);
            //Get Question 1 Vector 
            var questionEmbedding = await textEmbeddingGenerationService.GenerateAsync(question);
            Assert.IsNotNull(questionEmbedding);

            string id = Guid.NewGuid().ToString();
            string userIdNew = Guid.NewGuid().ToString();
            string sessionIdNew = Guid.NewGuid().ToString();
            // Add a document to delete
            var chatDocument = new ChatDocument
            {
                Id = id,
                UserId = userIdNew,
                Content = content,
                IpAddress = "127.0.0.1",
                SessionId = sessionIdNew,
                Timestamp = DateTime.UtcNow,
                QuestionVector = questionEmbedding.Vector,
                Question = question,
                Role = AuthorRole.User.Label
            };
            (_, bool addSuccess) = await chatHistoryDataService.AddDocumentAsync(chatDocument);
            Assert.IsTrue(addSuccess);

            Thread.Sleep(2000);
            // Delete by userId and sessionId
            bool deleteSuccess = await chatHistoryDataService.DeleteDocumentByUserIdAndSessionIdAsync(userIdNew, sessionIdNew);
            Assert.IsTrue(deleteSuccess);

            // Verify deletion

            var deleted = await chatHistoryDataService.FindAsync(x => x.UserId == userIdNew && x.SessionId == sessionIdNew);
            Assert.IsNull(deleted);
 
        }


        public async Task<IChatHistoryDataService> AddAndGetChatHistoryDataService(string question,string content,string localUserId, string localSessionId, string id)
        {
            IChatHistoryDataService chatHistoryDataService =
                AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
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
                Id = id,
                UserId = localUserId,
                Content = content,
                IpAddress = "127.0.0.1",
                SessionId = localSessionId,
                Timestamp = DateTime.UtcNow,
                QuestionVector = questionEmbedding.Vector,
                Question = question,
                Role = AuthorRole.User.Label
            };

            (_, bool success) = await chatHistoryDataService.AddDocumentAsync(chatDocument);
            Assert.IsTrue(success);
            return chatHistoryDataService;
        }

    }
}
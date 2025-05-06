
using Azure;
using ChatSessionManager.Helpers;
using ChatSessionManager.Models;
using ChatSessionManager.Models.Enums;
using ChatSessionManager.Models.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace ChatSessionManager
{
    public class CosmosChatHistoryDataService : ChatHistoryDataService
    {
        private readonly ILogger<CosmosChatHistoryDataService> _logger;
        readonly CosmosSearch _settings;
        private readonly CosmosClient _cosmosClient;
        private readonly CosmosSerializationOptions _cosmosSerializeOptions;
        public CosmosChatHistoryDataService(IOptions<ChatSessionManagerOptions> options, ILogger<CosmosChatHistoryDataService> logger) : base(logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.Value);
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
            _settings = options.Value.CosmosSearch;
            var (IsValid, message) = _settings.Validate();
            if (!IsValid)
                throw new ArgumentException(message);
            _cosmosSerializeOptions = new()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            };
            _cosmosClient = new CosmosClientBuilder(_settings.AccountEndpoint, _settings.AccountKey)
                .WithSerializerOptions(_cosmosSerializeOptions).Build();

        }
        /// <summary>
        /// Add Document to Db 
        /// </summary>
        /// <param name="chatDocument"></param>
        /// <returns></returns>
        public override async Task<(List<LogMessage> messages, bool success)> AddDocumentAsync(ChatDocument chatDocument)
        {
            List<LogMessage> messages = [];
            if (chatDocument == null)
            {
                messages.Add(new LogMessage($"{nameof(ChatDocument)} is required!", MessageType.Error));
                return (messages, false);
            }
            (bool IsValid, string message) = chatDocument.Validate();
            if (!IsValid)
            {
                messages.Add(new LogMessage($"{message}", MessageType.Error));
                return (messages, false);
            }
            try
            {
                (Container container, _, bool success) = await GetContainerAsync();
                if (container == null)
                {
                    return (messages, success);
                }
                // Store the document in Cosmos DB
                ItemResponse<ChatDocument> documentResponse = await container.CreateItemAsync(chatDocument, new PartitionKey(chatDocument.UserId));
                if (documentResponse.StatusCode != System.Net.HttpStatusCode.Created && documentResponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    messages.Add(new LogMessage($"Problem Added Document for UserId:{chatDocument.UserId}", MessageType.Info));
                    return (messages, false);
                }
                else
                {
                    messages.Add(new LogMessage($"Document Added Successfully for UserId:{chatDocument.UserId}", MessageType.Info));
                    return (messages, true);
                }

            }
            catch (Exception ex)
            {
                messages.Add(new LogMessage($"Error creating {nameof(ChatDocument)} Embedding: " + ex.Message, MessageType.Error));
            }
            PrintLogMessages(messages);
            //Create Index Document  
            return await Task.FromResult((messages, true));
        }



        public override async Task<List<ChatDocument>> FindAllAsync(Expression<Func<ChatDocument, bool>> predicate)
        {
            try
            {
                (Container container, List<LogMessage> logMessages, bool success) = await GetContainerAsync();
                if (container == null)
                {
                    return null;
                }
                var iterator = container.GetItemLinqQueryable<ChatDocument>(true)
                                  .Where(predicate)
                                  .ToFeedIterator();
                var results = new List<ChatDocument>();
                while (iterator.HasMoreResults)
                {
                    results.AddRange(await iterator.ReadNextAsync());
                }
                return results;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            return null;
        }

        public override async Task<ChatDocument> FindAsync(Expression<Func<ChatDocument, bool>> predicate)
        {
            (Container container, _, bool success) = await GetContainerAsync();
            if (container == null)
            {
                return null;
            }
            try
            {
                ChatDocument chatDocument = new();
                var iterator = container.GetItemLinqQueryable<ChatDocument>(true)
                                 .Where(predicate)
                                 .ToFeedIterator();
                while (iterator.HasMoreResults)
                {
                    foreach (var doc in await iterator.ReadNextAsync())
                    {
                        chatDocument = doc;
                        break;
                    }
                    break;
                }
                return chatDocument;

            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            return null;
        }

        public override async Task<HistoryContext> GetChatHistoryContextAsync(Expression<Func<ChatDocument, bool>> predicate)
        {
            var chatHistories = await FindAllAsync(predicate);
            if (chatHistories is not { Count: > 0 })
                return null;
            var historyContext = new HistoryContext();

            foreach (var chatHistory in chatHistories)
            {
                historyContext.AddHistory(chatHistory);
            }

            return historyContext;

        }

        public override async Task<HistoryContext> GetChatHistoryContextAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold)
        {
            var chatHistories = await GetDocumentsByQueryAsync(query, queryEmbeddings, size, userId, rerankerScoreThreshold);
            if (chatHistories is not { Count: > 0 })
                return null;
            var historyContext = new HistoryContext();
            foreach (var chatHistory in chatHistories)
            {
                historyContext.AddHistory(chatHistory);
            }
            return historyContext;
        }

        public override async Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold = 0.1)
        {
            (Container container, List<LogMessage> logMessages, bool success) = await GetContainerAsync();
            if (container == null)
            {
                return null;
            }
            try
            {
                string queryText = "SELECT Top " + size + @"   c.id, c.userId, c.question, c.content, 
        c.sessionId, c.timestamp, c.role, c.ipAddress, VectorDistance(c.questionVector, @questionVector, false) as similarityScore FROM c 
                                    WHERE c.userId = @userId ORDER BY c.similarityScore desc";

                var queryDef = new QueryDefinition(
                  query: queryText)
              .WithParameter("@questionVector", queryEmbeddings)
              .WithParameter("@userId", userId);
                // .WithParameter("@score",rerankerScoreThreshold);

                using FeedIterator<ChatDocument> resultSet = container.GetItemQueryIterator<ChatDocument>(queryDefinition: queryDef);

                List<ChatDocument> documents = [];
                while (resultSet.HasMoreResults)
                {
                    FeedResponse<ChatDocument> response = await resultSet.ReadNextAsync();
                    documents.AddRange(response);
                }

                return documents;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            return null;
        }

        public override async Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId)
        {

            (Container container, List<LogMessage> logMessages, bool success) = await GetContainerAsync();
            if (container == null)
            {
                return null;
            }
            try
            {
                var query = new QueryDefinition("Select * from c where c.userid= @userId")
                    .WithParameter("@userId", userId);
                var iterator = container.GetItemQueryIterator<ChatDocument>(query);
                var results = new List<ChatDocument>();
                while (iterator.HasMoreResults)
                {
                    results.AddRange(await iterator.ReadNextAsync());
                }
                return results;

            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            return null;
        }

        public override async Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId, string sessionId)
        {
            (Container container, List<LogMessage> logMessages, bool success) = await GetContainerAsync();

            if (container == null)
            {
                return null;
            }

            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND c.sessionId = @sessionId")
                    .WithParameter("@userId", userId)
                    .WithParameter("@sessionId", sessionId);

                var iterator = container.GetItemQueryIterator<ChatDocument>(query);
                var results = new List<ChatDocument>();

                while (iterator.HasMoreResults)
                {
                    results.AddRange(await iterator.ReadNextAsync());
                }

                return results;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "CosmosDB error occurred while searching");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while searching");
            }

            return null;
        }

        /// <summary>
        /// Create Data Source if not exists 
        /// </summary>
        /// <returns></returns>
        public override async Task<(List<LogMessage> messages, bool success)> CreateDataSourceIfNotExistAsync()
        {
            bool success = false;
            List<LogMessage> messages = [];
            try
            {
                success = await DataSourceExistsAsync();
                if (!success)
                {
                    (List<LogMessage> messages, bool success) logCreatedResponse = await CreateDataSourceAsync();
                    PrintLogMessages(logCreatedResponse.messages);
                    messages.AddRange(logCreatedResponse.messages);
                    success = logCreatedResponse.success;
                }
                else
                {
                    messages.Add(new LogMessage("Data source exists skipping creation!", MessageType.Info));
                    success = true;
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                messages.Add(new LogMessage(ex.Message, MessageType.Error));
            }
            PrintLogMessages(messages);
            return (messages, success);




        }
        /// <summary>
        /// Data Source Exists 
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> DataSourceExistsAsync()
        {
            try
            {
                Database _database = await _cosmosClient?.GetDatabase(_settings.DatabaseId).ReadAsync();
                if (_database == null)
                {
                    _logger.LogError($"Cosmos Database with Id:{_settings.DatabaseId} does not exist");
                    return false;
                }
                Container _container = await _database?.GetContainer(_settings.ContainerId).ReadContainerAsync();
                if (_container == null)
                {
                    _logger.LogError($"Cosmos Database with Id:{_settings.DatabaseId}, unable to connect to Container:{_settings.ContainerId}");
                    return false;
                }
                return await Task.FromResult(true);
            }
            catch (Exception f)
            {
                _logger?.LogError(f.Message);
                return false;
            }
        }
        /// <summary>
        /// Delete Data Source 
        /// </summary>
        /// <returns></returns>
        public override async Task<(List<LogMessage> messages, bool success)> DeleteIfDataSourceExistsAsync()
        {
            bool success = true;
            List<LogMessage> messages = [];

            try
            {
                Database _database = _cosmosClient?.GetDatabase(_settings.DatabaseId);
                DatabaseResponse databaseResponse = await _database.DeleteAsync(null);
                if (databaseResponse.StatusCode != System.Net.HttpStatusCode.NoContent && databaseResponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    messages.Add(new LogMessage($"Cosmos Database with Id:{_settings.DatabaseId} cannot be deleted,  Status code: {databaseResponse.StatusCode}", MessageType.Error));
                    success = false;
                    return (messages, success);
                }
                else
                {
                    messages.Add(new LogMessage($"Cosmos Database with Id:{_settings.DatabaseId}, Deleted Successfully", MessageType.Info));
                }

            }
            catch (Exception f)
            {
                _logger?.LogError(f.Message);

            }
            return (messages, success);

        }
        /// <summary>
        /// Create Data Source 
        /// </summary>
        /// <returns></returns>
        public async Task<(List<LogMessage> messages, bool success)> CreateDataSourceAsync()
        {
            bool success = true;
            List<LogMessage> messages = [];
            DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_settings.DatabaseId);
            if (databaseResponse.StatusCode != System.Net.HttpStatusCode.Created && databaseResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                messages.Add(new LogMessage($"Cosmos Database with Id:{_settings.DatabaseId} does not exist. Status code: {databaseResponse.StatusCode}", MessageType.Info));
                success = false;
                return (messages, success);
            }
            else
            {
                messages.Add(new LogMessage($"Cosmos Database with Id:{_settings.DatabaseId} creation Successfull", MessageType.Info));
            }
            ThroughputProperties throughputProperties = ThroughputProperties.CreateAutoscaleThroughput(1000);
            IndexingPolicy policy = new()
            {
                IndexingMode = IndexingMode.Consistent,
                VectorIndexes =
                [
                    new ()
                    {
                        Path="/questionVector",
                        Type= VectorIndexType.QuantizedFlat
                    }
                ]
            };

            VectorEmbeddingPolicy vectorEmbeddingPolicy = new(
                new Collection<Embedding>(
                [
                    new Embedding()
                {
                    Path = "/questionVector",
                    DataType = VectorDataType.Float32,
                    DistanceFunction = DistanceFunction.Cosine,
                    Dimensions = 1536
                }
                ]));

            ContainerProperties containerProperties = new(id: _settings.ContainerId, partitionKeyPath: "/userId")
            {
                DefaultTimeToLive = 86400,
                IndexingPolicy = policy,
                VectorEmbeddingPolicy = vectorEmbeddingPolicy
            };

            ContainerResponse containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerProperties, throughputProperties);
            if (containerResponse.StatusCode != System.Net.HttpStatusCode.Created && containerResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                messages.Add(new LogMessage($"Cosmos Database with Id:{_settings.DatabaseId}, Container:{_settings.ContainerId} does not exist . Status code: {containerResponse.StatusCode}", MessageType.Error));
                success = false;
                return (messages, success);
            }
            messages.Add(new LogMessage($"Cosmos Database with Id:{_settings.DatabaseId}, Container:{_settings.ContainerId}  creation Successfull", MessageType.Info));

            return (messages, success);
        }

        private async Task<(Container container, List<LogMessage> messages, bool success)> GetContainerAsync()
        {
            bool success = true;
            List<LogMessage> messages = [];
            Database _database = await _cosmosClient?.GetDatabase(_settings.DatabaseId).ReadAsync();
            if (_database == null)
            {
                messages.Add(new LogMessage($"Cosmos Database with Id:{_settings.DatabaseId} does not exist", MessageType.Error));
                return (null, messages, false);
            }
            Container _container = await _database?.GetContainer(_settings.ContainerId).ReadContainerAsync();
            if (_container == null)
            {
                messages.Add(new LogMessage($"Error Reading {nameof(ChatDocument)} Container:{_settings.ContainerId}: ", MessageType.Error));
                return (null, messages, false);
            }

            return (_container, messages, success);
        }
    }



}

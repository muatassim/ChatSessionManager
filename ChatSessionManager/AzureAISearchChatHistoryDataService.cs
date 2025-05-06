using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using ChatSessionManager.Helpers;
using ChatSessionManager.Models;
using ChatSessionManager.Models.Enums;
using ChatSessionManager.Models.Options;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using System.Net;

namespace ChatSessionManager
{
    public class AzureAISearchChatHistoryDataService : ChatHistoryDataService
    {
        readonly AzureAiSearch _settings;
        private readonly ILogger<AzureAISearchChatHistoryDataService> _logger;
        public AzureAISearchChatHistoryDataService(
            IOptions<ChatSessionManagerOptions> options, ILogger<AzureAISearchChatHistoryDataService> logger) : base(
                logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.Value);
            ArgumentNullException.ThrowIfNull(logger);
            _settings = options.Value.AzureAiSearch;
            _logger = logger;
            var (IsValid, message) = _settings.Validate();
            if (!IsValid)
                throw new ArgumentException(message);
        }

        /// <summary>
        /// Get Chat Documents by UserId
        /// </summary>
        /// <returns></returns>
        public override async Task<(List<LogMessage> messages, bool success)> DeleteIfDataSourceExistsAsync()
        {
            bool success = false;
            List<LogMessage> messages = [];
            AzureKeyCredential _credential = new(_settings.ApiKey);
            SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
            bool indexExists = false;
            try
            {
                Task<Response<SearchIndex>> response = _searchIndexClient.GetIndexAsync(_settings.IndexName);
                if (response is { Result.Value: not null })
                {
                    indexExists = true;
                }
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    messages.Add(new LogMessage("Index not found", MessageType.Info));
                }
            }
            catch (Exception ex)
            {
                messages.Add(new LogMessage("Error: " + ex.Message, MessageType.Info));
                indexExists = false;
            }
            if (indexExists)
            {
                await _searchIndexClient.DeleteIndexAsync(_settings.IndexName);
                success = true;
                messages.Add(new LogMessage($"{nameof(ChatDocument)} Index Successfully Deleted! ", MessageType.Info));
            }
            PrintLogMessages(messages);
            return (messages, success);
        }
        /// <summary>
        /// Create If not exist 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override async Task<(List<LogMessage> messages, bool success)> CreateDataSourceIfNotExistAsync()
        {
            bool success = false;
            List<LogMessage> messages = [];
            try
            {
                success = await DataSourceExistsAsync();
                if (!success)
                {
                    (List<LogMessage> messages, bool success) logCreatedResponse = await CreateIndexAsync();
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
        /// Get Chat History Context
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get History Context
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryEmbeddings"></param>
        /// <param name="size"></param>
        /// <param name="userId"></param>
        /// <param name="rerankerScoreThreshold"></param>
        /// <returns></returns>
        public override async Task<HistoryContext> GetChatHistoryContextAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold = 3.5)
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

        /// <summary>
        /// Get Chat Documents by UserId
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> DataSourceExistsAsync()
        {
            bool indexExists = false;
            List<LogMessage> messages = [];
            AzureKeyCredential _credential = new(_settings.ApiKey);
            SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
            try
            {
                Response<SearchIndex> response = await _searchIndexClient.GetIndexAsync(_settings.IndexName);
                if (response.Value != null)
                {
                    indexExists = true;
                }
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    _logger.LogError("Index not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            PrintLogMessages(messages);
            return indexExists;
        }

        /// <summary>
        /// Get Chat Documents by UserId
        /// </summary>
        /// <param name="chatDocument"></param>
        /// <returns></returns>
        public override async Task<(List<LogMessage> messages, bool success)> AddDocumentAsync(ChatDocument chatDocument)
        {
            bool success = false;
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

                AzureKeyCredential _credential = new(_settings.ApiKey);
                SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
                SearchClient searchClient = _searchIndexClient.GetSearchClient(_settings.IndexName);
                IndexDocumentsBatch<SearchDocument> batch = new();
                SearchDocument searchDocument = new()
                {
                        { nameof(ChatDocument.Id).Camelize(), chatDocument.Id },
                        { nameof(ChatDocument.UserId).Camelize(), chatDocument.UserId },
                        { nameof(ChatDocument.Content).Camelize(), chatDocument.Content },
                        { nameof(ChatDocument.IpAddress).Camelize(), chatDocument.IpAddress },
                        { nameof(ChatDocument.SessionId).Camelize(), chatDocument.SessionId },
                        { nameof(ChatDocument.Timestamp).Camelize(), chatDocument.Timestamp },
                        { nameof(ChatDocument.Role).Camelize(), chatDocument.Role },
                        { nameof(ChatDocument.Question).Camelize(), chatDocument.Question },
                        { nameof(ChatDocument.QuestionVector).Camelize(), chatDocument.QuestionVector.ToArray() },
                };
                batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(IndexActionType.MergeOrUpload, searchDocument));
                IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch);
                if (result.Results[0].Status == (int)HttpStatusCode.OK || result.Results[0].Status == (int)HttpStatusCode.Created)
                {
                    success = true;
                    messages.Add(new LogMessage($"{nameof(ChatDocument)} with Id:{chatDocument.Id} added Successfully to Index!", MessageType.Info));
                }
                else
                {
                    messages.Add(new LogMessage($"Error creating {nameof(ChatDocument)}  with Id:{{chatDocument.Id}} added Successfully to Index! {result.Results[0].ErrorMessage}", MessageType.Error));
                }

                success = true;
                messages.Add(new LogMessage($"{nameof(ChatDocument)} Embedding Successfully created!", MessageType.Info));
            }
            catch (Exception ex)
            {
                messages.Add(new LogMessage($"Error creating {nameof(ChatDocument)} Embedding: " + ex.Message, MessageType.Error));
            }
            PrintLogMessages(messages);
            //Create Index Document  
            return await Task.FromResult((messages, success));
        }




        /// <summary>
        /// Get Chat Documents by UserId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public override async Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId)
        {
            List<ChatDocument> chatDocuments = [];
            AzureKeyCredential _credential = new(_settings.ApiKey);
            SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
            SearchClient searchClient = _searchIndexClient.GetSearchClient(_settings.IndexName);
            SearchOptions options = new()
            {
                Filter = $"userId eq '{userId}'",
                SearchMode = SearchMode.All,
                QueryType = SearchQueryType.Full
            };
            try
            {

                Response<SearchResults<ChatDocument>> response = await searchClient.SearchAsync<ChatDocument>("*", options);
                if (response.GetRawResponse().Status == (int)HttpStatusCode.OK)
                {
                    foreach (SearchResult<ChatDocument> result in response.Value.GetResults())
                    {
                        chatDocuments.Add(result.Document);
                    }
                    return chatDocuments;
                }

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
        public override async Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId,string sessionId)
        {
            List<ChatDocument> chatDocuments = [];
            AzureKeyCredential _credential = new(_settings.ApiKey);
            SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
            SearchClient searchClient = _searchIndexClient.GetSearchClient(_settings.IndexName);
            SearchOptions options = new()
            {
                Filter = $"userId eq '{userId}' and sessionId eq '{sessionId}'",
                SearchMode = SearchMode.All,
                QueryType = SearchQueryType.Full
            };
            try
            {

                Response<SearchResults<ChatDocument>> response = await searchClient.SearchAsync<ChatDocument>("*", options);
                if (response.GetRawResponse().Status == (int)HttpStatusCode.OK)
                {
                    foreach (SearchResult<ChatDocument> result in response.Value.GetResults())
                    {
                        chatDocuments.Add(result.Document);
                    }
                    return chatDocuments;
                }

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
        /// <summary>
        /// Get Chat Documents by Query, Hybrid Search using Vector and Semantic Search
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryEmbeddings"></param>
        /// <param name="size"></param>
        /// <param name="userId"></param> 
        /// <param name="rerankerScoreThreshold"></param>
        /// <returns></returns>
        public override async Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold = 3.5)
        {
            var chatDocuments = new List<ChatDocument>();
            try
            {
                AzureKeyCredential _credential = new(_settings.ApiKey);
                SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
                SearchClient searchClient = _searchIndexClient.GetSearchClient(_settings.IndexName);

                if (queryEmbeddings != null)
                {
                    var options = new SearchOptions
                    {
                        VectorSearch = new()
                        {
                            Queries = {
                                new VectorizedQuery(queryEmbeddings.Value.ToArray())
                                {
                                    KNearestNeighborsCount = 50,
                                    Fields = { nameof(ChatDocument.QuestionVector).Camelize() }
                                }
                            }
                        },
                        SemanticSearch = new SemanticSearchOptions()
                        {
                            SemanticConfigurationName = _settings.SemanticSearchConfigName,
                            QueryCaption = new(QueryCaptionType.Extractive),
                            QueryAnswer = new(QueryAnswerType.Extractive),
                        },
                        QueryType = SearchQueryType.Semantic, // Tells the Azure Cognitive Search to use Semantic Ranking 
                        Size = size,
                        Select = { "*" }
                    };

                    try
                    {
                        Response<SearchResults<ChatDocument>> response = await searchClient.SearchAsync<ChatDocument>(query, options);

                        if (response.GetRawResponse().Status == (int)HttpStatusCode.OK)
                        {
                            foreach (SearchResult<ChatDocument> result in response.Value.GetResults())
                            {
                                if (result?.Document?.UserId == userId
                                    && result?.SemanticSearch.RerankerScore >= rerankerScoreThreshold)
                                {
                                    chatDocuments.Add(result.Document);
                                }
                            }
                            return chatDocuments;
                        }
                    }
                    catch (RequestFailedException ex)
                    {
                        _logger.LogError(ex, "Error occurred while searching");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while searching");
                    }
                }

                return null;
            }
            catch (NullReferenceException)
            {
                _logger.LogInformation("Total Results: 0");
            }
            return null;
        }

        public override async Task<List<ChatDocument>> FindAllAsync(Expression<Func<ChatDocument, bool>> predicate)
        {
            try
            {
                AzureKeyCredential _credential = new(_settings.ApiKey);
                SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
                SearchClient _searchClient = _searchIndexClient.GetSearchClient(_settings.IndexName);
                var results = await _searchClient.SearchAsync<ChatDocument>(string.Empty, new SearchOptions
                {
                    QueryType = SearchQueryType.Full,
                    Size = 1000 // Adjust size as needed, or handle paging
                });

                var chatDocuments = results.Value.GetResults()
                    .Select(result => result.Document)
                    .AsQueryable()
                    .Where(predicate)
                    .ToList();

                return chatDocuments;

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
            try
            {
                AzureKeyCredential _credential = new(_settings.ApiKey);
                SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
                SearchClient _searchClient = _searchIndexClient.GetSearchClient(_settings.IndexName);
                // Use LINQ to query the search client or a similar method to fulfill the predicate
                var results = await _searchClient.SearchAsync<ChatDocument>(string.Empty, new SearchOptions
                {
                    QueryType = SearchQueryType.Full
                });

                var chatDocument = results.Value.GetResults()
                    .Select(result => result.Document)
                    .AsQueryable()
                    .FirstOrDefault(predicate);

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
        #region //private methods


        /// <summary>
        /// Get Chat Documents by UserId
        /// </summary>
        /// <returns></returns>
        async Task<(List<LogMessage> messages, bool success)> CreateIndexAsync()
        {
            bool success = false;
            var messages = new List<LogMessage>();

            try
            {
                AzureKeyCredential _credential = new(_settings.ApiKey);
                SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);

                var index = new SearchIndex(_settings.IndexName)
                {
                    Fields =
                    [
                        new SimpleField(nameof(ChatDocument.Id).Camelize(), SearchFieldDataType.String) { IsKey = true },
                        new SimpleField(nameof(ChatDocument.UserId).Camelize(), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                        new SearchableField(nameof(ChatDocument.Content).Camelize()) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                        new SearchableField(nameof(ChatDocument.IpAddress).Camelize()) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                        new SearchableField(nameof(ChatDocument.SessionId).Camelize()) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                        new SimpleField(nameof(ChatDocument.Timestamp).Camelize(), SearchFieldDataType.DateTimeOffset) { IsFilterable = true },
                        new SearchableField(nameof(ChatDocument.Role).Camelize()) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                        new SearchableField(nameof(ChatDocument.Question).Camelize()) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                        new VectorSearchField(nameof(ChatDocument.QuestionVector).Camelize(), _settings.ModelDimension, _settings.VectorSearchProfile)
                    ],
                    VectorSearch = new()
                    {
                        Profiles = { new VectorSearchProfile(_settings.VectorSearchProfile, _settings.VectorSearchHNSWConfig) },
                        Algorithms = { new HnswAlgorithmConfiguration(_settings.VectorSearchHNSWConfig) }
                    },
                    SemanticSearch = new()
                    {
                        Configurations =
                        {
                           new SemanticConfiguration(_settings.SemanticSearchConfigName, new()
                           {
                               TitleField = new SemanticField(nameof(ChatDocument.Question).Camelize()),
                               ContentFields =
                                  {
                                   new SemanticField(nameof(ChatDocument.Question).Camelize()),
                                   new SemanticField(nameof(ChatDocument.Content).Camelize()),
                                   new SemanticField(nameof(ChatDocument.UserId).Camelize()),
                                   new SemanticField(nameof(ChatDocument.SessionId).Camelize())
                                  },
                               KeywordsFields =
                                  {
                                    new SemanticField(nameof(ChatDocument.Question).Camelize()),
                                    new SemanticField(nameof(ChatDocument.UserId).Camelize())
                                  }
                           })
                        },
                    },
                    Suggesters = { new SearchSuggester(nameof(ChatDocument.Question).Camelize(), [nameof(ChatDocument.Content).Camelize()]) }
                };

                Response<SearchIndex> response = await _searchIndexClient.CreateOrUpdateIndexAsync(index);
                if (response.GetRawResponse().Status == (int)HttpStatusCode.Created || response.GetRawResponse().Status == (int)HttpStatusCode.OK)
                {
                    success = true;
                    messages.Add(new LogMessage($"{nameof(ChatDocument)} Index Successfully created!", MessageType.Info));
                }
                else
                {
                    messages.Add(new LogMessage($"Error creating {nameof(ChatDocument)} Index: " + response.GetRawResponse().ReasonPhrase, MessageType.Error));
                }
            }
            catch (Exception ex)
            {
                messages.Add(new LogMessage($"Error creating {nameof(ChatDocument)} Index: " + ex.Message, MessageType.Error));
            }

            return (messages, success);
        }

        #endregion

    }



}

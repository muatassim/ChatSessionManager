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
        private readonly AzureAiSearch _settings;
        private readonly ILogger<AzureAISearchChatHistoryDataService> _logger;
        private readonly SearchIndexClient _searchIndexClient;
        private readonly SearchClient _searchClient;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        public AzureAISearchChatHistoryDataService(
            IOptions<ChatSessionManagerOptions> options, ILogger<AzureAISearchChatHistoryDataService> logger) : base(logger)
        {
            ArgumentNullException.ThrowIfNull(options?.Value);
            ArgumentNullException.ThrowIfNull(logger);
            _settings = options.Value.AzureAiSearch;
            _logger = logger;
            var (IsValid, message) = _settings.Validate();
            if (!IsValid)
                throw new ArgumentException(message);

            var credential = new AzureKeyCredential(_settings.ApiKey);
            _searchIndexClient = new SearchIndexClient(_settings.SearchUrl, credential);
            _searchClient = _searchIndexClient.GetSearchClient(_settings.IndexName);
        }
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;
            await _initSemaphore.WaitAsync();
            try
            {
                if (!_isInitialized)
                {
                    var (_, success) = await CreateDataSourceIfNotExistAsync();
                    _isInitialized = success;
                }
            }
            finally
            {
                _initSemaphore.Release();
            }
        }
        public override async Task<(List<LogMessage> messages, bool success)> DeleteIfDataSourceExistsAsync()
        {
            await EnsureInitializedAsync();
            var messages = new List<LogMessage>();
            bool success = false;
            try
            {
                var response = await _searchIndexClient.GetIndexAsync(_settings.IndexName);
                if (response?.Value != null)
                {
                    await _searchIndexClient.DeleteIndexAsync(_settings.IndexName);
                    success = true;
                    messages.Add(new LogMessage($"{nameof(ChatDocument)} Index Successfully Deleted!", MessageType.Info));
                }
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                messages.Add(new LogMessage("Index not found", MessageType.Info));
            }
            catch (Exception ex)
            {
                messages.Add(new LogMessage("Error: " + ex.Message, MessageType.Error));
            }
            PrintLogMessages(messages);
            return (messages, success);
        }

        public override async Task<(List<LogMessage> messages, bool success)> CreateDataSourceIfNotExistAsync()
        {
            var messages = new List<LogMessage>();
            bool success = false;
            try
            {
                success = await DataSourceExistsAsync();
                if (!success)
                {
                    var (logMessages, logSuccess) = await CreateIndexAsync();
                    PrintLogMessages(logMessages);
                    messages.AddRange(logMessages);
                    success = logSuccess;
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

        public override async Task<HistoryContext> GetChatHistoryContextAsync(Expression<Func<ChatDocument, bool>> predicate)
        {
            await EnsureInitializedAsync();
            var chatHistories = await FindAllAsync(predicate) ?? [];
            if (chatHistories.Count == 0)
                return null;
            var historyContext = new HistoryContext();
            foreach (var chatHistory in chatHistories)
                historyContext.AddHistory(chatHistory);
            return historyContext;
        }

        public override async Task<HistoryContext> GetChatHistoryContextAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold = 3.5)
        {
            await EnsureInitializedAsync();
            var chatHistories = await GetDocumentsByQueryAsync(query, queryEmbeddings, size, userId, rerankerScoreThreshold) ?? [];
            if (chatHistories.Count == 0)
                return null;
            var historyContext = new HistoryContext();
            foreach (var chatHistory in chatHistories)
                historyContext.AddHistory(chatHistory);
            return historyContext;
        }

        public override async Task<bool> DeleteDocumentAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            try
            {
                await EnsureInitializedAsync();
                var document = await FindAsync(d => d.Id == id);
                if (document == null)
                    return false;

                var batch = IndexDocumentsBatch.Delete("id", [id]);
                var result = await _searchClient.IndexDocumentsAsync(batch);
                return result.Value.Results.All(r => r.Succeeded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting document with id: {id}");
                return false;
            }
        }

        public override async Task<bool> DeleteDocumentByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            try
            {
                await EnsureInitializedAsync();
                var documents = await GetDocumentsByUserIdAsync(userId) ?? [];
                if (documents.Count == 0)
                    return true;

                var ids = documents.Select(d => d.Id).ToList();
                var batch = IndexDocumentsBatch.Delete("id", ids);
                var result = await _searchClient.IndexDocumentsAsync(batch);
                return result.Value.Results.All(r => r.Succeeded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting documents for userId: {userId}");
                return false;
            }
        }

        public override async Task<bool> DeleteDocumentByUserIdAndSessionIdAsync(string userId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
                return false;

            try
            {
                await EnsureInitializedAsync();
                var documents = await GetDocumentsByUserIdAndSessionIdAsync(userId, sessionId) ?? [];
                if (documents.Count == 0)
                    return true;

                var ids = documents.Select(d => d.Id).ToList();
                var batch = IndexDocumentsBatch.Delete("id", ids);
                var result = await _searchClient.IndexDocumentsAsync(batch);
                return result.Value.Results.All(r => r.Succeeded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting documents for userId: {userId} and sessionId: {sessionId}");
                return false;
            }
        }

        public override async Task<bool> DataSourceExistsAsync()
        {
            try
            {
                var response = await _searchIndexClient.GetIndexAsync(_settings.IndexName);
                return response?.Value != null;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogError("Index not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return false;
        }

        public override async Task<(List<LogMessage> messages, bool success)> AddDocumentAsync(ChatDocument chatDocument)
        {
            var messages = new List<LogMessage>();
            if (chatDocument == null)
            {
                messages.Add(new LogMessage($"{nameof(ChatDocument)} is required!", MessageType.Error));
                return (messages, false);
            }
            var (IsValid, message) = chatDocument.Validate();
            if (!IsValid)
            {
                messages.Add(new LogMessage(message, MessageType.Error));
                return (messages, false);
            }
            try
            {
                await EnsureInitializedAsync();
                var searchDocument = new SearchDocument
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
                var batch = IndexDocumentsBatch.MergeOrUpload([searchDocument]);
                IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);
                if (result.Results[0].Status == (int)HttpStatusCode.OK || result.Results[0].Status == (int)HttpStatusCode.Created)
                {
                    messages.Add(new LogMessage($"{nameof(ChatDocument)} with Id:{chatDocument.Id} added Successfully to Index!", MessageType.Info));
                    messages.Add(new LogMessage($"{nameof(ChatDocument)} Embedding Successfully created!", MessageType.Info));
                    return (messages, true);
                }
                messages.Add(new LogMessage($"Error creating {nameof(ChatDocument)} with Id:{chatDocument.Id}: {result.Results[0].ErrorMessage}", MessageType.Error));
            }
            catch (Exception ex)
            {
                messages.Add(new LogMessage($"Error creating {nameof(ChatDocument)} Embedding: " + ex.Message, MessageType.Error));
            }
            PrintLogMessages(messages);
            return (messages, false);
        }

        public override async Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId)
        {
            var chatDocuments = new List<ChatDocument>();
            var options = new SearchOptions
            {
                Filter = $"userId eq '{userId}'",
                SearchMode = SearchMode.All,
                QueryType = SearchQueryType.Full
            };
            try
            {
                var response = await _searchClient.SearchAsync<ChatDocument>("*", options);
                if (response.GetRawResponse().Status == (int)HttpStatusCode.OK)
                {
                    chatDocuments.AddRange(response.Value.GetResults().Select(r => r.Document));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            return chatDocuments;
        }

        public override async Task<List<ChatDocument>> GetDocumentsByUserIdAndSessionIdAsync(string userId, string sessionId)
        {
            var chatDocuments = new List<ChatDocument>();
            var options = new SearchOptions
            {
                Filter = $"userId eq '{userId}' and sessionId eq '{sessionId}'",
                SearchMode = SearchMode.All,
                QueryType = SearchQueryType.Full
            };
            try
            {
                var response = await _searchClient.SearchAsync<ChatDocument>("*", options);
                if (response.GetRawResponse().Status == (int)HttpStatusCode.OK)
                {
                    chatDocuments.AddRange(response.Value.GetResults().Select(r => r.Document));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            return chatDocuments;
        }

        public override async Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold = 3.5)
        {
            var chatDocuments = new List<ChatDocument>();
            if (queryEmbeddings == null)
                return chatDocuments;

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
                QueryType = SearchQueryType.Semantic,
                Size = size,
                Select = { "*" }
            };

            try
            {
                var response = await _searchClient.SearchAsync<ChatDocument>(query, options);
                if (response.GetRawResponse().Status == (int)HttpStatusCode.OK)
                {
                    foreach (var result in response.Value.GetResults())
                    {
                        if (result?.Document?.UserId == userId && result?.SemanticSearch.RerankerScore >= rerankerScoreThreshold)
                        {
                            chatDocuments.Add(result.Document);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            return chatDocuments;
        }

        public override async Task<List<ChatDocument>> FindAllAsync(Expression<Func<ChatDocument, bool>> predicate)
        {
            try
            {
                var results = await _searchClient.SearchAsync<ChatDocument>(string.Empty, new SearchOptions
                {
                    QueryType = SearchQueryType.Full,
                    Size = 1000
                });

                return [.. results.Value.GetResults()
                    .Select(result => result.Document)
                    .AsQueryable()
                    .Where(predicate)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            return [];
        }

        public override async Task<ChatDocument> FindAsync(Expression<Func<ChatDocument, bool>> predicate)
        {
            try
            {
                var results = await _searchClient.SearchAsync<ChatDocument>(string.Empty, new SearchOptions
                {
                    QueryType = SearchQueryType.Full
                });

                return results.Value.GetResults()
                    .Select(result => result.Document)
                    .AsQueryable()
                    .FirstOrDefault(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching");
            }
            return null;
        }

        async Task<(List<LogMessage> messages, bool success)> CreateIndexAsync()
        {
            var messages = new List<LogMessage>();
            bool success = false;
            try
            {
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

                var response = await _searchIndexClient.CreateOrUpdateIndexAsync(index);
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
    }
}
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Indexes;
using Azure;
using System.Net;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Microsoft.Extensions.Logging;
using ChatSessionManager.AzureAiSearchChatSession.Models.Options;
using Microsoft.Extensions.Options;
using ChatSessionManager.AzureAiSearchChatSession.Models;
using ChatSessionManager.AzureAiSearchChatSession.Models.Enums;
using Humanizer;

namespace ChatSessionManager.AzureAiSearchChatSession
{
    public class AzureAiSearchChatHistoryDataService : ChatHistoryDataService
    {
        readonly AzureAiSearch _settings;
        private readonly ILogger<AzureAiSearchChatHistoryDataService> _logger;
        public AzureAiSearchChatHistoryDataService(
            IOptions<ChatSessionManagerOptions> options, ILogger<AzureAiSearchChatHistoryDataService> logger) : base(
                logger)
        {
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
        public async override Task<(List<LogMessage> messages, bool success)> DeleteIfDataSourceExistsAsync()
        {
            bool success = false;
            List<LogMessage> messages = [];
            AzureKeyCredential _credential = new(_settings.ApiKey);
            SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
            bool indexExists = false;
            try
            {
                Task<Response<SearchIndex>> response = _searchIndexClient.GetIndexAsync(_settings.IndexName);
                if (response != null && response.Result != null && response.Result.Value != null)
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
            return (messages, success);
        }
        /// <summary>
        /// Create If not exist 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async override Task<(List<LogMessage> messages, bool success)> CreateDataSourceIfNotExistAsync()
        {
            bool success = false;
            List<LogMessage> messages = [];
            try
            {
                success= await DataSourceExistsAsync();
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
            return (messages, success);

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

            return indexExists;
        }

        /// <summary>
        /// Get Chat Documents by UserId
        /// </summary>
        /// <param name="chatDocument"></param>
        /// <returns></returns>
        public async override Task<(List<LogMessage> messages, bool success)> AddDocumentAsync(ChatDocument chatDocument)
        {
            bool success = false;
            List<LogMessage> messages = [];
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
                        { nameof(ChatDocument.Message).Camelize(), chatDocument.Message },
                        { nameof(ChatDocument.QuestionVector).Camelize(), chatDocument.QuestionVector.ToArray() },
                        { nameof(ChatDocument.CreatedAt).Camelize(), chatDocument.CreatedAt }
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
            //Create Index Document  
            return await Task.FromResult((messages, success));
        }




        /// <summary>
        /// Get Chat Documents by UserId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async override Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId)
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
        /// <summary>
        /// Get Chat Documents by Query Does the Text Search on Message Field
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async override Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query)
        {
            List<ChatDocument> chatDocuments = [];
            AzureKeyCredential _credential = new(_settings.ApiKey);
            SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
            SearchClient searchClient = _searchIndexClient.GetSearchClient(_settings.IndexName);
            SearchOptions options = new()
            {
                Filter = $"search.ismatch('{query}', '{nameof(ChatDocument.Message).Camelize()}')",
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
        /// <returns></returns>
        public async override Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size)
        {
            List<ChatDocument> chatDocuments = [];
            try
            {

                AzureKeyCredential _credential = new(_settings.ApiKey);
                SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
                SearchClient searchClient = _searchIndexClient.GetSearchClient(_settings.IndexName);
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
                    QueryType = SearchQueryType.Semantic, //tells the Azure CognitiveSearch to use Semantic Rankig 
                    Size = size
                };
                try
                {

                    Response<SearchResults<ChatDocument>> response = await searchClient.SearchAsync<ChatDocument>(query, options);
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
            catch (NullReferenceException)
            {
                _logger.LogInformation("Total Results: 0");
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

            List<LogMessage> messages = [];
            try
            {
                AzureKeyCredential _credential = new(_settings.ApiKey);
                SearchIndexClient _searchIndexClient = new(_settings.SearchUrl, _credential);
                SearchIndex index = new(_settings.IndexName)
                {
                    Fields =
                    [
                           new SimpleField(nameof(ChatDocument.Id).Camelize(), SearchFieldDataType.String) { IsKey = true },
                           new SimpleField(nameof(ChatDocument.UserId).Camelize(), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true },
                           new SearchableField(nameof(ChatDocument.Question).Camelize()) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                           new SearchableField(nameof(ChatDocument.Message).Camelize()) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                           new VectorSearchField(nameof(ChatDocument.QuestionVector).Camelize(), _settings.ModelDimension, _settings.VectorSearchProfile),
                           new SimpleField(nameof(ChatDocument.CreatedAt).Camelize(), SearchFieldDataType.DateTimeOffset){ IsFilterable = true }
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
                                    new SemanticField(nameof(ChatDocument.Message).Camelize())
                                  } ,
                                  KeywordsFields=
                                  {
                                    new SemanticField(nameof(ChatDocument.Question).Camelize()),
                                    new SemanticField(nameof(ChatDocument.UserId).Camelize())
                                  }
                           })
                        },
                    },
                    Suggesters = { new SearchSuggester(nameof(ChatDocument.Question).Camelize(), [nameof(ChatDocument.Message).Camelize()]) }
                };
                Response<SearchIndex> response = await _searchIndexClient.CreateOrUpdateIndexAsync(index);
                if (response.GetRawResponse().Status == (int)HttpStatusCode.Created || response.GetRawResponse().Status == (int)HttpStatusCode.OK)
                {
                    success = true;
                    messages.Add(new LogMessage($"{nameof(ChatDocument)} Index Successfully created! ", MessageType.Info));
                }
                else
                {
                    messages.Add(new LogMessage($"Error creating {nameof(ChatDocument)} Index: " + response.GetRawResponse().ReasonPhrase, MessageType.Error));
                }
            }
            catch (Exception ex)
            {
                messages.Add(new LogMessage($"Error creating {nameof(ChatDocument)} Embedding: " + ex.Message, MessageType.Error));

            }
            return (messages, success);
        }


        #endregion

    }



}

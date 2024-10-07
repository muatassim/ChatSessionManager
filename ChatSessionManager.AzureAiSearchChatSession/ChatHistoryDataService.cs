using Microsoft.Extensions.Logging;
using ChatSessionManager.AzureAiSearchChatSession.Interfaces;
using ChatSessionManager.AzureAiSearchChatSession.Models;
using ChatSessionManager.AzureAiSearchChatSession.Models.Enums;

namespace ChatSessionManager.AzureAiSearchChatSession
{
    public abstract class ChatHistoryDataService  : IChatHistoryDataService
    {
        protected ILogger<AzureAISearchChatHistoryDataService> Logger { get; }
        public ChatHistoryDataService(ILogger<AzureAISearchChatHistoryDataService> logger)
        {
             Logger = logger;
        }

        protected void PrintLogMessages(List<LogMessage> messages)
        {
            if (messages == null || messages.Count == 0) return;
            foreach (LogMessage message in messages)
            {
                switch (message.MessageType)
                {
                    case MessageType.Error:
                        Logger.LogError(message.Message);
                    break;
                    case MessageType.Info:
                        Logger.LogInformation(message.Message);
                    break;
                    case MessageType.Warning:
                        Logger.LogWarning(message.Message);
                        break;
                }
            }
        }
        /// <summary>
        /// Create DataSource 
        /// </summary>
        /// <returns></returns>
        public abstract Task<(List<LogMessage> messages, bool success)> CreateDataSourceIfNotExistAsync();

        /// <summary>
        /// DataSource Exists 
        /// </summary>
        /// <returns></returns>
        public abstract Task<bool> DataSourceExistsAsync();

        public abstract Task<(List<LogMessage> messages, bool success)> DeleteIfDataSourceExistsAsync();

        /// <summary>
        /// Add document
        /// </summary>
        /// <param name="chatDocument"></param>
        /// <returns></returns>

        public abstract Task<(List<LogMessage> messages, bool success)> AddDocumentAsync(ChatDocument chatDocument);

        /// <summary>
        /// Get Documents 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public abstract Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query);

        /// <summary>
        /// Get documents 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryEmbeddings"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public abstract Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size);

        /// <summary>
        /// Get 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public abstract Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId);
    }



}

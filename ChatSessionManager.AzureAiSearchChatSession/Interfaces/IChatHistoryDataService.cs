using ChatSessionManager.AzureAiSearchChatSession.Models;

namespace ChatSessionManager.AzureAiSearchChatSession.Interfaces
{
    public interface IChatHistoryDataService
    {
        /// <summary>
        /// Delete if Data Source Exists 
        /// </summary>
        /// <returns></returns>
        Task<(List<LogMessage> messages, bool success)> DeleteIfDataSourceExistsAsync();
        /// <summary>
        /// Create DataSource If not Exists 
        /// </summary>
        /// <returns></returns>
        Task<(List<LogMessage> messages, bool success)> CreateDataSourceIfNotExistAsync();
        /// <summary>
        /// Check if Data Source Exists 
        /// </summary>
        /// <returns></returns>
        Task<bool> DataSourceExistsAsync();
        /// <summary>
        /// Add document to datasource 
        /// </summary>
        /// <param name="chatDocument"></param>
        /// <returns></returns>
        Task<(List<LogMessage> messages, bool success)> AddDocumentAsync(ChatDocument chatDocument); 
        /// <summary>
        /// Get chat
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query);
        /// <summary>
        /// Get 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryEmbeddings"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size);
        /// <summary>
        /// Get Document by UserId 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId);
    }
}
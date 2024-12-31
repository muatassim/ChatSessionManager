using ChatSessionManager.Models;
using System.Linq.Expressions;

namespace ChatSessionManager.Interfaces
{
    public interface IChatHistoryDataService
    {

        /// <summary>
        /// Add document to datasource 
        /// </summary>
        /// <param name="chatDocument"></param>
        /// <returns></returns>
        Task<(List<LogMessage> messages, bool success)> AddDocumentAsync(ChatDocument chatDocument);

        /// <summary>
        /// Create DataSource If not Exists 
        /// </summary>
        /// <returns></returns>
        Task<(List<LogMessage> messages, bool success)> CreateDataSourceIfNotExistAsync();

        /// <summary>
        /// Delete if Data Source Exists 
        /// </summary>
        /// <returns></returns>
        Task<(List<LogMessage> messages, bool success)> DeleteIfDataSourceExistsAsync();
        /// <summary>
        /// Check if Data Source Exists 
        /// </summary>
        /// <returns></returns>
        Task<bool> DataSourceExistsAsync();
        /// <summary>
        /// Get 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<ChatDocument> FindAsync(Expression<Func<ChatDocument, bool>> predicate);


        /// <summary>
        /// Get 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<List<ChatDocument>> FindAllAsync(Expression<Func<ChatDocument, bool>> predicate);

        /// <summary>
        /// Get ChatHistory Context 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<HistoryContext> GetChatHistoryContextAsync(Expression<Func<ChatDocument, bool>> predicate);


        /// <summary>
        /// Get History Context 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryEmbeddings"></param>
        /// <param name="size"></param>
        /// <param name="userId"></param>
        /// <param name="rerankerScoreThreshold"></param>
        /// <returns></returns>
        Task<HistoryContext> GetChatHistoryContextAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold);

        /// <summary>
        /// Get Documents
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryEmbeddings"></param>
        /// <param name="size"></param>
        /// <param name="userId"></param>
        /// <param name="rerankerScoreThreshold"></param>
        /// <returns></returns>
        Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold = 3.5);
        /// <summary>
        /// Get Document by UserId 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId);






    }
}
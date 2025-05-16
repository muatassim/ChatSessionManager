using ChatSessionManager.Interfaces;
using ChatSessionManager.Models;
using ChatSessionManager.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace ChatSessionManager.Helpers
{
    public abstract class ChatHistoryDataService(ILogger<ChatHistoryDataService> logger) : IChatHistoryDataService
    {
        protected ILogger<ChatHistoryDataService> Logger { get; } = logger;

        /// <summary>
        /// Print to Log
        /// </summary>
        /// <param name="messages"></param>
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
        /// Get 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public abstract Task<ChatDocument> FindAsync(Expression<Func<ChatDocument, bool>> predicate);


        /// <summary>
        /// Find All 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public abstract Task<List<ChatDocument>> FindAllAsync(Expression<Func<ChatDocument, bool>> predicate);



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
        /// <summary>
        /// Delete DataSource If Exists
        /// </summary>
        /// <returns></returns>
        public abstract Task<(List<LogMessage> messages, bool success)> DeleteIfDataSourceExistsAsync();

        /// <summary>
        /// Add document
        /// </summary>
        /// <param name="chatDocument"></param>
        /// <returns></returns>

        public abstract Task<(List<LogMessage> messages, bool success)> AddDocumentAsync(ChatDocument chatDocument);

        /// <summary>
        /// Get Document 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryEmbeddings"></param>
        /// <param name="size"></param>
        /// <param name="userId"></param>
        /// <param name="rerankerScoreThreshold">reranker score do not apply to cosmos, provide if needed only for azure ai search</param>
        /// <returns></returns>

        public abstract Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold = 3.5);


        /// <summary>
        /// Get 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public abstract Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId);
        /// <summary>
        /// Get by UserId and SessionId
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public abstract Task<List<ChatDocument>> GetDocumentsByUserIdAndSessionIdAsync(string userId, string sessionId);

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>

        public abstract Task<HistoryContext> GetChatHistoryContextAsync(Expression<Func<ChatDocument, bool>> predicate);

        /// <summary>
        /// Get 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="queryEmbeddings"></param>
        /// <param name="size"></param>
        /// <param name="rerankerScoreThreshold"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public abstract Task<HistoryContext> GetChatHistoryContextAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold);


        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract Task<bool> DeleteDocumentAsync(string id);
        /// <summary>
        /// Delete Document by UserId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public abstract Task<bool> DeleteDocumentByUserIdAsync(string userId);
        /// <summary>
        /// Delete Document by UserId and SessionId
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public abstract Task<bool> DeleteDocumentByUserIdAndSessionIdAsync(string userId, string sessionId);

    }

}

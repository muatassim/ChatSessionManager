using ChatSessionManager.Interfaces;
using ChatSessionManager.Models;
using ChatSessionManager.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace ChatSessionManager.Helpers
{
    public abstract class ChatHistoryDataService : IChatHistoryDataService
    {
        protected ILogger<ChatHistoryDataService> Logger { get; }
        public ChatHistoryDataService(ILogger<ChatHistoryDataService> logger)
        {
            Logger = logger;
        }
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
        /// Cosine Similarity
        /// </summary>
        /// <param name="vectorA"></param>
        /// <param name="vectorB"></param>
        /// <returns></returns>
        protected double CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += Math.Pow(vectorA[i], 2);
                magnitudeB += Math.Pow(vectorB[i], 2);
            }

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }

    }

}

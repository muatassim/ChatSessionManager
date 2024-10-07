using System.Text;

namespace ChatSessionManager.AzureAiSearchChatSession.Models
{
    public class HistoryContext
    {
        public List<ChatDocument> ChatHistories { get; set; }

        public HistoryContext()
        {
            ChatHistories = new List<ChatDocument>();
        }

        public void AddHistory(ChatDocument chatHistory)
        {
            ChatHistories.Add(chatHistory);
        }

        public override string ToString()
        {
            var historyContext = new StringBuilder();
            foreach (var history in ChatHistories)
            {
                historyContext.AppendLine($"Question: {history.Question}");
                historyContext.AppendLine($"Response: {history.Content}");
            }
            return historyContext.ToString();
        }
    }

}

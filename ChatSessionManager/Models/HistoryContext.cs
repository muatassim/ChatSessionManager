using System.Text;

namespace ChatSessionManager.Models
{
    public class HistoryContext
    {
        public List<ChatDocument> ChatHistories { get; set; } = [];

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

using ChatSessionManager.AzureAiSearchChatSession.Models.Enums;
using System.Text;
using System.Text.Json.Serialization;

namespace ChatSessionManager.AzureAiSearchChatSession.Models
{
    public class LogMessage
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("createdOn")]
        public DateTime CreatedOn { get; set; }

        [JsonPropertyName("messageType")]
        public MessageType MessageType { get; set; }

        public LogMessage()
        {
        }

        public LogMessage(string message, MessageType messageType)
        {
            CreatedOn = DateTime.Now;
            Message = message;
            MessageType = messageType;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            StringBuilder stringBuilder2 = stringBuilder;
            StringBuilder.AppendInterpolatedStringHandler handler = new(14, 3, stringBuilder2);
            handler.AppendFormatted(Message);
            handler.AppendLiteral(" :>>>>>>>>>>>");
            handler.AppendFormatted(CreatedOn);
            handler.AppendLiteral(" ");
            handler.AppendFormatted(Environment.NewLine);
            stringBuilder2.AppendLine(ref handler);
            return stringBuilder.ToString();
        }


    }
}

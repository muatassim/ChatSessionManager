using System.Text.Json.Serialization;

namespace ChatSessionManager.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageType
    {
        Info,
        Error,
        Warning

    }
}

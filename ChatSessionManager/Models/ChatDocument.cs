using ChatSessionManager.Models.Options;
using System.Text.Json.Serialization;

namespace ChatSessionManager.Models
{


    public class ChatDocument
    {
        [JsonPropertyName("id")]
        /// <summary>
        /// Id of the document
        /// </summary>
        public string Id { get; set; }


        [JsonPropertyName("userId")]
        /// <summary>
        /// User Id
        /// </summary>
        public string UserId { get; set; }

        [JsonPropertyName("content")]
        /// <summary>
        /// Content
        /// </summary>

        public string Content { get; set; }

        [JsonPropertyName("ipAddress")]
        /// <summary>
        /// IpAddress 
        /// </summary>
        public string IpAddress { get; set; }


        [JsonPropertyName("sessionId")]
        /// <summary>
        /// GEt 
        /// </summary>
        public string SessionId { get; set; }


        [JsonPropertyName("timestamp")]
        /// <summary>
        /// Created At
        /// </summary>

        public DateTime Timestamp { get; set; }

        [JsonPropertyName("role")]
        /// <summary>
        /// Role of the message 
        /// </summary>
        public string Role { get; set; }


        [JsonPropertyName("questionVector")]

        /// <summary>
        /// Question Vector
        /// </summary>

        public ReadOnlyMemory<float> QuestionVector { get; set; }


        [JsonPropertyName("question")]
        /// <summary>
        /// Question 
        /// </summary>

        public string Question { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, UserId: {UserId}, Content: {Content}, CreatedAt: {Timestamp}";
        }

        public (bool IsValid, string message) Validate()
        {
            var IsValid = true;
            List<string> errors = [];

            if (string.IsNullOrEmpty(Id))
            {
                IsValid = false;
                errors.Add($"{nameof(AzureAiSearch)} Validation error: {nameof(Id)} is Required");
            }
            if (string.IsNullOrEmpty(UserId))
            {
                IsValid = false;
                errors.Add($"{nameof(AzureAiSearch)} Validation error: {nameof(UserId)} is Required");
            }
            if (string.IsNullOrEmpty(Question))
            {
                IsValid = false;
                errors.Add($"{nameof(AzureAiSearch)} Validation error: {nameof(Question)} is Required");
            }


            return (IsValid, string.Join(", ", errors));
        }
    }
}

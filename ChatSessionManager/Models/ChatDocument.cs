using ChatSessionManager.Models.Options;
using System.Text.Json.Serialization;

namespace ChatSessionManager.Models
{


    public class ChatDocument
    {
        /// <summary>
        /// id
        /// </summary>
        [JsonPropertyName("id")]
         
        public string Id { get; set; }

        /// <summary>
        /// User Id 
        /// </summary>
        [JsonPropertyName("userId")] 
        public string UserId { get; set; }


        /// <summary>
        /// content 
        /// </summary>
        [JsonPropertyName("content")] 

        public string Content { get; set; }

        /// <summary>
        /// ip address 
        /// </summary>
        [JsonPropertyName("ipAddress")] 
        public string IpAddress { get; set; }


        /// <summary>
        /// sessionId
        /// </summary>
        [JsonPropertyName("sessionId")] 
        public string SessionId { get; set; }


        /// <summary>
        /// timestamp
        /// </summary>
        [JsonPropertyName("timestamp")] 

        public DateTime Timestamp { get; set; }

        /// <summary>
        /// role 
        /// </summary>
        [JsonPropertyName("role")] 
        public string Role { get; set; }

        /// <summary>
        /// questionVector
        /// </summary>
        [JsonPropertyName("questionVector")] 
        public ReadOnlyMemory<float> QuestionVector { get; set; }

        /// <summary>
        /// Question 
        /// </summary>

        [JsonPropertyName("question")] 

        public string Question { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, UserId: {UserId}, Content: {Content}, CreatedAt: {Timestamp}";
        }
        /// <summary>
        /// Validate
        /// </summary>
        /// <returns></returns>
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

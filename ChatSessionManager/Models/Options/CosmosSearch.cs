using System.Text.Json.Serialization;

namespace ChatSessionManager.Models.Options
{
    public class CosmosSearch
    {
        /// <summary>
        /// Database Id
        /// </summary>
        [JsonPropertyName("databaseId")]
        public string DatabaseId { get; set; }

        /// <summary>
        /// Container 
        /// </summary>

        [JsonPropertyName("containerId")]
        public string ContainerId { get; set; }


        /// <summary>
        /// Account Endpoint 
        /// </summary>
        [JsonPropertyName("accountEndpoint")]
        public string AccountEndpoint { get; set; }

        /// <summary>
        /// Account Key 
        /// </summary>

        [JsonPropertyName("accountKey")]
        public string AccountKey { get; set; }



        public (bool IsValid, string message) Validate()
        {
            var IsValid = true;
            List<string> errors = [];

            if (string.IsNullOrEmpty(DatabaseId))
            {
                IsValid = false;
                errors.Add($"{nameof(CosmosSearch)} Validation error: {nameof(DatabaseId)} is Required");
            }
            if (string.IsNullOrEmpty(ContainerId))
            {
                IsValid = false;
                errors.Add($"{nameof(CosmosSearch)} Validation error: {nameof(ContainerId)} is Required");
            }
            if (string.IsNullOrEmpty(AccountEndpoint))
            {
                IsValid = false;
                errors.Add($"{nameof(CosmosSearch)} Validation error: {nameof(AccountEndpoint)} is Required");
            }

            if (string.IsNullOrEmpty(AccountKey))
            {
                IsValid = false;
                errors.Add($"{nameof(CosmosSearch)} Validation error: {nameof(AccountKey)} is Required");
            }

            return (IsValid, string.Join(", ", errors));
        }



    }
}

namespace ChatSessionManager.Models.Options
{
    public class AzureAiSearch
    {

        public AzureAiSearch() { }

        /// <summary>
        /// Service Name 
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Api Key 
        /// </summary>
        public string ApiKey { get; set; }


        public Uri SearchUrl => new($"https://{ServiceName}.search.windows.net");


        public string SemanticSearchConfigName { get; set; }

        public string VectorSearchProfile { get; set; }

        public string VectorSearchHNSWConfig { get; set; }

        public int ModelDimension { get; set; }

        public string IndexName
        {
            get { return indexName; }
            set { indexName = value.ToLower(); }
        }

        private string indexName;


        public (bool IsValid, string message) Validate()
        {
            var IsValid = true;
            List<string> errors = [];

            if (string.IsNullOrEmpty(ServiceName))
            {
                IsValid = false;
                errors.Add($"{nameof(AzureAiSearch)} Validation error: {nameof(ServiceName)} is Required");
            }
            if (string.IsNullOrEmpty(ApiKey))
            {
                IsValid = false;
                errors.Add($"{nameof(AzureAiSearch)} Validation error: {nameof(ApiKey)} is Required");
            }

            if (string.IsNullOrEmpty(IndexName))
            {
                IsValid = false;
                errors.Add($"{nameof(AzureAiSearch)} Validation error: {nameof(IndexName)} is Required");
            }

            return (IsValid, string.Join(", ", errors));
        }

    }
}

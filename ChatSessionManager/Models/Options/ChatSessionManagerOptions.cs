﻿namespace ChatSessionManager.Models.Options
{
    public class ChatSessionManagerOptions
    {
        public ChatSessionManagerOptions() { }

        public AzureAiSearch AzureAiSearch { get; set; }

        public CosmosSearch CosmosSearch { get; set; }

        /// <summary>
        /// Validate 
        /// </summary>
        /// <returns></returns>
        public (bool IsValid, string message) Validate()
        {
            var IsValid = true;
            List<string> errors = [];
            if (AzureAiSearch != null)
            {
                (bool IsValid, string message) searchValidate = AzureAiSearch.Validate();
                if (!searchValidate.IsValid)
                {
                    IsValid = false;
                }
                if (!string.IsNullOrEmpty(searchValidate.message))
                {
                    errors.Add(searchValidate.message);
                }
            }
            if (CosmosSearch != null)
            {
                (bool IsValid, string message) searchValidate = CosmosSearch.Validate();
                if (!searchValidate.IsValid)
                {
                    IsValid = false;
                }
                if (!string.IsNullOrEmpty(searchValidate.message))
                {
                    errors.Add(searchValidate.message);
                }
            }

            return (IsValid, string.Join(Environment.NewLine, errors));
        }
    }
}

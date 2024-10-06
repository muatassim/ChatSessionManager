namespace ChatSessionManager.AzureAiSearchChatSession.Models
{

    
    public class ChatDocument
    {
        /// <summary>
        /// Id of the document
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public string UserId { get; set; }


        /// <summary>
        /// Message
        /// </summary>

        public string Message { get; set; }

        /// <summary>
        /// Created At
        /// </summary>

        public DateTime CreatedAt { get; set; }


        /// <summary>
        /// Question Vector
        /// </summary>

        public ReadOnlyMemory<float> QuestionVector { get; set; }

        /// <summary>
        /// Question 
        /// </summary>

        public string Question { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, UserId: {UserId}, Message: {Message}, CreatedAt: {CreatedAt}";
        }
    }
}

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
        /// Content
        /// </summary>

        public string Content { get; set; }


        /// <summary>
        /// IpAddress 
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// GEt 
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Created At
        /// </summary>

        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Role of the message 
        /// </summary>
        public string Role { get; set; }


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
            return $"Id: {Id}, UserId: {UserId}, Content: {Content}, CreatedAt: {Timestamp}";
        }
    }
}

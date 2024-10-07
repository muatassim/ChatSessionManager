# ChatSessionManager 

## Overview
**ChatHistoryManager** is a robust and scalable solution designed to store and query chat history data for AI applications. This repository contains the core components necessary to seamlessly integrate chat session storage into your AI-driven applications.

## Features
- **Efficient Data Storage:** Store chat sessions in a structured format.
- **Easy Retrieval:** Query and retrieve chat sessions quickly.
- **Scalability:** Designed to handle large volumes of chat data.
- **Extensible:** Easily integrate with various AI frameworks and tools.
- **Secure:** Ensure data integrity and security.

## Installation
1. **Clone the repository:**
    ```sh
    git clone https://github.com/muatassim/ChatSessionManager.git
    ```

2. **Navigate to the project directory:**
    ```sh
    cd ChatSessionManager
    ```

3. **Install dependencies:**
    ```sh
    dotnet restore
    ```
4. **Add to Dependency Injection: **
    ```csharp
     services.AddAzureAISearchChatHistory(context.Configuration);
    ```


### Configuration

Add the following settings to your `appsettings.json` file:

```json
{
  "ChatSessionManagerOptions": {
    "AzureAiSearch": {
      "ServiceName": "--Azure Ai Service Name--",  
      "ApiKey": "--API KEY--",
      "SemanticSearchConfigName": "my-semantic-config",
      "VectorSearchHNSWConfig": "my-hnsw-vector-config",
      "VectorSearchProfile": "my-vector-profile",
      "ModelDimension": "1536",
      "IndexName": "INDEX NAME"
    }
  }
}
```

### Usage 
To integrate the `AzureAISearchChatHistoryDataService` implementation of the `IChatHistoryDataService` interface, follow these steps:



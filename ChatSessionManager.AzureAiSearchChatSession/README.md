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
     //For Adding AzureAISearch 
     services.AddAzureAISearchChatHistory(context.Configuration);
    ```

# Chat History Data Service

The `IChatHistoryDataService` interface provides various methods for managing and querying chat documents in a data source. This guide explains how to use each method with examples.
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
## Interface Methods

### AddDocumentAsync

Adds a document to the data source.

## Method Signature:
```csharp
Task<(List<LogMessage> messages, bool success)> AddDocumentAsync(ChatDocument chatDocument);
```
Example:
```csharp
var chatDocument = new ChatDocument { UserId = "user123", Content = "Hello, world!" };
var (messages, success) = await chatHistoryDataService.AddDocumentAsync(chatDocument);
if (success) Console.WriteLine("Document added successfully");
```

### CreateDataSourceIfNotExistAsync
Creates the DataSource
## Method Signature
```csharp
Task<(List<LogMessage> messages, bool success)> CreateDataSourceIfNotExistAsync();
```
Example: 
```csharp
var (messages, success) = await chatHistoryDataService.CreateDataSourceIfNotExistAsync();
if (success) Console.WriteLine("Data source created successfully");
```

### DeleteIfDataSourceExistsAsync
Delete the DataSource if it exists 

## Method Signature
```csharp
Task<(List<LogMessage> messages, bool success)> DeleteIfDataSourceExistsAsync();
```
Example:
```csharp
var (messages, success) = await chatHistoryDataService.DeleteIfDataSourceExistsAsync();
if (success) Console.WriteLine("Data source deleted successfully");
```

### DataSourceExistsAsync
Check if Data Source Exists 

## Method Signature
```csharp
Task<bool> DataSourceExistsAsync();
```
Example:
```csharp
var exists = await chatHistoryDataService.DataSourceExistsAsync();
Console.WriteLine($"Data source exists: {exists}");
```

### FindAsync
Find Records by predicate 
## Method Signature
```csharp

Task<ChatDocument> FindAsync(Expression<Func<ChatDocument, bool>> predicate);
```
Example: 
```csharp 
var chatDocument = await chatHistoryDataService.FindAsync(doc => doc.UserId == "user123");
Console.WriteLine($"Found document: {chatDocument?.Content}");

 Expression<Func<ChatDocument, bool>> expr = x => x.Question == question;
 var document = await chatHistoryDataService.FindAsync(expr);
Console.WriteLine($"Found document: {document?.Content}");
```
### FindAllAsync
Find all related records based on predicate

## Method signature

```csharp
Task<List<ChatDocument>> FindAllAsync(Expression<Func<ChatDocument, bool>> predicate);
```
Example:
```csharp
var chatDocuments = await chatHistoryDataService.FindAllAsync(doc => doc.UserId == "user123");
foreach (var doc in chatDocuments)
{
    Console.WriteLine($"Found document: {doc.Content}");
}
```
### GetChatHistoryContextAsync 
Get Chat History Context which can be added to IChatCompletion 
## Method Signature
```csharp
Task<HistoryContext> GetChatHistoryContextAsync(Expression<Func<ChatDocument, bool>> predicate);
```
Example:
```csharp
var historyContext = await chatHistoryDataService.GetChatHistoryContextAsync(doc => doc.UserId == "user123");
Console.WriteLine($"History context: {historyContext}");
Task<HistoryContext> GetChatHistoryContextAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold);
var query = "What is the capital of France?";
var queryEmbeddings = new ReadOnlyMemory<float>(/* Embedding values */);
int size = 10;
string userId = "user123";
double rerankerScoreThreshold = 3.5;

var historyContext = await chatHistoryDataService.GetChatHistoryContextAsync(query, queryEmbeddings, size, userId, rerankerScoreThreshold);
Console.WriteLine($"History context: {historyContext}");

//Add to ChatCompletion 
 IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
 if (historyContext != null)
 {
     chatHistory.AddMessage(AuthorRole.Assistant, historyContext.ToString());
 }
```
### GetDocumentsByQueryAsync 
Get documents by Query for the user 
## Method Signature 
```csharp
Task<List<ChatDocument>> GetDocumentsByQueryAsync(string query, ReadOnlyMemory<float>? queryEmbeddings, int size, string userId, double rerankerScoreThreshold = 3.5);
```
  
Example:
```csharp
 
var query = "What is the capital of France?";
var queryEmbeddings = new ReadOnlyMemory<float>(/* Embedding values */);
int size = 10;
string userId = "user123";
double rerankerScoreThreshold = 3.5;

var chatDocuments = await chatHistoryDataService.GetDocumentsByQueryAsync(query, queryEmbeddings, size, userId, rerankerScoreThreshold);
foreach (var doc in chatDocuments)
{
    Console.WriteLine($"Question: {doc.Question}, Answer: {doc.Content}");
}

```

### GetDocumentsByUserIdAsync
Get documents by UserId
## Method Signature
```csharp
Task<List<ChatDocument>> GetDocumentsByUserIdAsync(string userId);

```

Example:
```csharp
 
var chatDocuments = await chatHistoryDataService.GetDocumentsByUserIdAsync("user123");
foreach (var doc in chatDocuments)
{
    Console.WriteLine($"Question: {doc.Question}, Answer: {doc.Content}");
}


```




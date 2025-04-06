# ChatSessionManager

## Overview
**ChatHistoryManager** is a robust and highly scalable solution designed to streamline the storage and retrieval of chat history in AI-driven applications. It offers a structured approach to managing conversational data, ensuring both efficiency and security. It supports multiple implementations, including **Azure AI Search** and **Cosmos DB**.

## Features
- **Optimized Data Storage:** Captures and organizes chat sessions in a structured format for seamless access.
- **Rapid Querying & Retrieval:** Enables swift searches and retrieval of past interactions, enhancing AI contextual awareness.
- **Enterprise-Grade Scalability:** Designed to handle high chat volume, making it ideal for AI solutions that require large-scale conversational data.
- **Flexible Integration:** Works effortlessly with diverse AI frameworks and tools, allowing easy adaptation for various applications.
- **Secure & Reliable::** Maintains data integrity and security, ensuring sensitive chat data is well protected.
- **Multi-Backend Support:** Supports multiple implementations, including **Azure AI Search** and **Cosmos DB**, offering adaptability based on infrastructure needs.


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
   
4. **Add to Dependency Injection:**
    ```csharp
     //For Adding AzureAISearch and AzureAiSearch opitons value : Required
     services.AddAzureAISearchChatHistory(context.Configuration);
     //For Cosmos and CosmosSearch configuration is required
     services.AddCosmosChatHistory(context.Configuration);
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
    },
     "CosmosSearch": {
        "DatabaseId": "Database Id",
        "ContainerId": "Container ",
        "AccountEndpoint": "Cosmos End Point",
        "AccountKey": "Account Key"
     }
  }
}

```

### Usage
To integrate IChatHistoryDataService in your project. Follwoing implementation can be used

1. `AzureAISearchChatHistoryDataService` which utilizes Azure AI Search:
   - **Constructor**: `[FromKeyedServices(nameof(AzureAISearchChatHistoryDataService))] IChatHistoryDataService dataService`
2. `AzureCosmosSearchChatHistoryDataService` which utilizes Cosmos DB on preview: for details refer: https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/vector-search 
   - **Constructor**: `[FromKeyedServices(nameof(CosmosChatHistoryDataService))] IChatHistoryDataService dataService`


## Semantic Kernel Chat Example 
This section shows how to use the Semantic Kernel chat with history. The example demonstrates how to ask a series of questions where the context from previous answers is important.

```csharp
[TestMethod]
[DataRow("I'm planning a trip to Paris. Can you tell me the best time of year to visit and some must-see attractions?",
    "Given that I'm interested in art and history, what are some lesser-known museums in Paris that I should visit?")] 
[DataRow("I want to adopt a healthier diet. Can you suggest some nutritious foods to incorporate into my meals?",
   "Based on the foods you suggested, can you give me a simple recipe for a balanced meal?")] 
[DataRow("I'm looking for a good mystery novel to read. Can you suggest one?", "Sounds interesting. What can you tell me about the main character in 'The Girl with the Dragon Tattoo'?")]
[DataRow("I want to start a workout routine to build muscle. Any tips on what exercises I should do?", "Can you suggest a weekly workout plan that includes those exercises?")]
public async Task ChatWithHistoryExampleAsync_Test(string question, string followUpQuestion)
{

    IOptions<AzureOpenAIOptions> options = AppHost.GetServiceProvider().GetRequiredService<IOptions<AzureOpenAIOptions>>();
    Assert.IsNotNull(options);
    //var openAI = new OpenAI(options.Value.Key, options.Value.Endpoint);
    AzureOpenAIOptions azureOpenAIOptions = options.Value as AzureOpenAIOptions;

    IChatHistoryDataService chatHistoryDataService = AppHost.GetServiceProvider().GetKeyedService<IChatHistoryDataService>(nameof(AzureAISearchChatHistoryDataService));
    Assert.IsNotNull(chatHistoryDataService);

    Kernel kernel = AppHost.GetServiceProvider().GetService<Kernel>();

    Assert.IsNotNull(kernel);

    IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    Assert.IsNotNull(chatCompletionService); 
    ITextEmbeddingGenerationService textEmbeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    Assert.IsNotNull(textEmbeddingGenerationService);
    //Get Question 1 Vector 
    ReadOnlyMemory<float> questionEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(question);
    Assert.IsNotNull(questionEmbedding);

    await AskQuestion(question, chatHistoryDataService, chatCompletionService,questionEmbedding);


    //Second Question Vector 
    ReadOnlyMemory<float> followUpQuestionEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(followUpQuestion);
    Assert.IsNotNull(followUpQuestionEmbedding);


    //Start next Question 
    await AskQuestion(followUpQuestion, chatHistoryDataService, chatCompletionService, followUpQuestionEmbedding);
    //Get History Records 


    //Send the Question Again 

}

private static async Task AskQuestion(string question, 
    IChatHistoryDataService chatHistoryDataService,
    IChatCompletionService chatCompletionService,
    ReadOnlyMemory<float> questionEmbedding )
{
    ///Question 1 Record 
    ChatHistory chatHistory = [];
    chatHistory.AddSystemMessage("You are an AI assistant who answers the users questions in a thoughtfull manner and are precise with your answer.");
    //Add history and usermessage 
    var historyContext = await chatHistoryDataService.GetChatHistoryContextAsync(question,questionEmbedding,2,userId,0.5);
    if (historyContext != null)
    {
        chatHistory.AddMessage(AuthorRole.Assistant, historyContext.ToString());
    }
    chatHistory.AddUserMessage(question);

    ChatMessageContent messageContent = await chatCompletionService.GetChatMessageContentAsync(question);
    //Save Question 1 and Response 
   // await SaveChat(question, chatHistoryDataService, questionEmbedding, chatHistory, messageContent);
}
private static async Task SaveChat(string question, IChatHistoryDataService chatHistoryDataService, ReadOnlyMemory<float> questionEmbedding, ChatHistory chatHistory, ChatMessageContent messageContent)
{
    ChatDocument chatDocument = new()
    {
        Id = Guid.NewGuid().ToString(),
        UserId = userId,
        Content = messageContent.Content,
        IpAddress = "127.0.0.1",
        SessionId = sessionId,
        Timestamp = DateTime.UtcNow,
        QuestionVector = questionEmbedding,
        Question = question,
        Role = AuthorRole.User.Label
    };
    chatHistory.Add(messageContent);
    //Save the conversation to the UserStore 
    (List<LogMessage> messages, bool success) response = await chatHistoryDataService.AddDocumentAsync(chatDocument);
    Assert.IsNotNull(response);
}


```
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




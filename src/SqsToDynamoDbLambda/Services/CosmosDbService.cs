using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SqsToDynamoDbLambda.Models;
using System.Text.Json;

namespace SqsToDynamoDbLambda.Services;

/// <summary>
/// Implementation of the Cosmos DB service
/// </summary>
public class CosmosDbService : IDocumentDbService
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<CosmosDbService> _logger;
    private readonly string _databaseName;
    private readonly string _containerName;
    private Container _container;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public CosmosDbService(ILogger<CosmosDbService> logger)
    {
        _logger = logger;

        // Get configuration from environment variables or use defaults
        var connectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString") ?? 
            throw new InvalidOperationException("CosmosDbConnectionString environment variable not set");
        
        _databaseName = Environment.GetEnvironmentVariable("CosmosDbDatabaseName") ?? "MessageDatabase";
        _containerName = Environment.GetEnvironmentVariable("CosmosDbContainerName") ?? "Messages";

        // Initialize Cosmos client and container
        _cosmosClient = new CosmosClient(connectionString);
        _container = _cosmosClient.GetContainer(_databaseName, _containerName);
    }

    /// <summary>
    /// Constructor for testing with mocked dependencies
    /// </summary>
    public CosmosDbService(CosmosClient cosmosClient, ILogger<CosmosDbService> logger, string databaseName, string containerName)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
        _databaseName = databaseName;
        _containerName = containerName;
        _container = _cosmosClient.GetContainer(_databaseName, _containerName);
    }

    /// <inheritdoc/>
    public async Task WriteMessageToCosmosDbAsync(ServiceBusReceivedMessage message)
    {
        if (message == null)
        {
            _logger.LogWarning("Received null Service Bus message");
            return;
        }

        try
        {
            // Create a document to store message attributes and body
            var messageData = new ServiceBusMessageData
            {
                Id = Guid.NewGuid().ToString(), // Generate a unique ID for Cosmos DB
                MessageId = message.MessageId,
                Body = message.Body.ToString(),
                ContentType = message.ContentType,
                CorrelationId = message.CorrelationId,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            // Add user properties if any
            if (message.ApplicationProperties.Count > 0)
            {
                var propertiesJson = JsonSerializer.Serialize(message.ApplicationProperties);
                messageData.UserProperties = propertiesJson;
            }

            // Try parsing the body as JSON to store enriched data
            try
            {
                // Validate if body is JSON to provide more information in log
                JsonDocument.Parse(message.Body.ToString());
                _logger.LogInformation("Message body is valid JSON");
            }
            catch (JsonException)
            {
                _logger.LogInformation("Message body is not valid JSON, storing as string");
            }

            // Write to Cosmos DB
            var response = await _container.CreateItemAsync(messageData);
            _logger.LogInformation($"Successfully wrote message {message.MessageId} to Cosmos DB container {_containerName} with request charge {response.RequestCharge} RUs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error writing message {message.MessageId} to Cosmos DB");
            throw; // Re-throw to handle in the calling function
        }
    }
}
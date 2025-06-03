using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ServiceBusToCosmosDbFunction.Services;

/// <summary>
/// Implementation of the Cosmos DB service
/// </summary>
public class CosmosDbService : ICosmosDbService
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<CosmosDbService> _logger;
    private readonly string _databaseName;
    private readonly string _containerName;
    private Container? _container;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public CosmosDbService(ILogger<CosmosDbService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get configuration values
        var connectionString = configuration["CosmosDbConnectionString"] ?? 
            throw new InvalidOperationException("CosmosDbConnectionString is required");
        _databaseName = configuration["CosmosDbDatabaseName"] ?? "MessageDatabase";
        _containerName = configuration["CosmosDbContainerName"] ?? "ServiceBusMessages";

        _cosmosClient = new CosmosClient(connectionString);
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
            // Initialize container if needed
            await EnsureContainerAsync();

            // Create message document similar to DynamoDB structure
            var messageDocument = new Dictionary<string, object>
            {
                { "id", message.MessageId },
                { "MessageId", message.MessageId },
                { "Body", message.Body.ToString() },
                { "ContentType", message.ContentType ?? string.Empty },
                { "CorrelationId", message.CorrelationId ?? string.Empty },
                { "Label", message.Subject ?? string.Empty },
                { "TimeToLive", message.TimeToLive.TotalSeconds },
                { "Timestamp", DateTime.UtcNow.ToString("o") }
            };

            // Add message properties if any
            if (message.ApplicationProperties != null && message.ApplicationProperties.Count > 0)
            {
                var propertiesJson = JsonSerializer.Serialize(message.ApplicationProperties);
                messageDocument.Add("ApplicationProperties", propertiesJson);
            }

            // Try parsing the body as JSON to store structured data if possible
            try
            {
                var bodyJson = JsonSerializer.Deserialize<JsonElement>(message.Body.ToString());
                // If successful, store the parsed JSON properties
                var bodyElements = new Dictionary<string, object>();
                ExtractJsonElements(bodyJson, bodyElements, "BodyJson");
                
                foreach (var element in bodyElements)
                {
                    messageDocument.Add(element.Key, element.Value);
                }
            }
            catch (JsonException)
            {
                // If not valid JSON, just continue with the body as string
                _logger.LogInformation("Message body is not valid JSON, storing as string");
            }

            // Write to Cosmos DB
            await _container!.CreateItemAsync(messageDocument, new PartitionKey(message.MessageId));
            _logger.LogInformation($"Successfully wrote message {message.MessageId} to Cosmos DB container {_containerName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error writing message {message.MessageId} to Cosmos DB");
            throw; // Re-throw to handle in the calling function
        }
    }

    /// <summary>
    /// Ensure the Cosmos DB container exists
    /// </summary>
    private async Task EnsureContainerAsync()
    {
        if (_container == null)
        {
            var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
            var containerResponse = await database.Database.CreateContainerIfNotExistsAsync(
                _containerName,
                "/MessageId", // Partition key
                400); // Default RUs
            _container = containerResponse.Container;
        }
    }

    /// <summary>
    /// Helper method to extract JSON elements recursively (similar to AWS implementation)
    /// </summary>
    private void ExtractJsonElements(JsonElement element, Dictionary<string, object> attributes, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var newPrefix = $"{prefix}.{property.Name}";
                    ExtractJsonElements(property.Value, attributes, newPrefix);
                }
                break;
            case JsonValueKind.Array:
                var arrayJson = JsonSerializer.Serialize(element);
                attributes.Add(prefix, arrayJson);
                break;
            case JsonValueKind.String:
                attributes.Add(prefix, element.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Number:
                attributes.Add(prefix, element.GetDecimal());
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                attributes.Add(prefix, element.GetBoolean());
                break;
            case JsonValueKind.Null:
                attributes.Add(prefix, (object?)null!);
                break;
            default:
                attributes.Add(prefix, element.GetRawText());
                break;
        }
    }
}
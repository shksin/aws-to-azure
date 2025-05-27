using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SqsToDynamoDbLambda.Services;

/// <summary>
/// Implementation of the DynamoDB service
/// </summary>
public class DynamoDbService : IDynamoDbService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly ILogger<DynamoDbService> _logger;
    private readonly string _tableName;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public DynamoDbService(ILogger<DynamoDbService> logger)
    {
        _dynamoDbClient = new AmazonDynamoDBClient();
        _logger = logger;

        // Get table name from environment variable or use default
        _tableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME") ?? "SqsMessages";
    }

    /// <summary>
    /// Constructor for testing with mocked dependencies
    /// </summary>
    public DynamoDbService(IAmazonDynamoDB dynamoDbClient, ILogger<DynamoDbService> logger, string tableName)
    {
        _dynamoDbClient = dynamoDbClient;
        _logger = logger;
        _tableName = tableName;
    }

    /// <inheritdoc/>
    public async Task WriteMessageToDynamoDbAsync(SQSEvent.SQSMessage record)
    {
        if (record == null)
        {
            _logger.LogWarning("Received null SQS message record");
            return;
        }

        try
        {
            // Create a dictionary to store message attributes and body
            var messageData = new Dictionary<string, AttributeValue>
            {
                { "MessageId", new AttributeValue { S = record.MessageId } },
                { "ReceiptHandle", new AttributeValue { S = record.ReceiptHandle } },
                { "Body", new AttributeValue { S = record.Body } },
                { "Md5OfBody", new AttributeValue { S = record.Md5OfBody } },
                { "Timestamp", new AttributeValue { S = DateTime.UtcNow.ToString("o") } }
            };

            // Add message attributes if any
            if (record.MessageAttributes != null && record.MessageAttributes.Count > 0)
            {
                var attributesJson = JsonSerializer.Serialize(record.MessageAttributes);
                messageData.Add("MessageAttributes", new AttributeValue { S = attributesJson });
            }

            // Try parsing the body as JSON to store structured data if possible
            try
            {
                var bodyJson = JsonSerializer.Deserialize<JsonElement>(record.Body);
                // If successful, store the parsed JSON properties
                var bodyElements = new Dictionary<string, AttributeValue>();
                ExtractJsonElements(bodyJson, bodyElements, "BodyJson");
                
                foreach (var element in bodyElements)
                {
                    messageData.Add(element.Key, element.Value);
                }
            }
            catch (JsonException)
            {
                // If not valid JSON, just continue with the body as string
                _logger.LogInformation("Message body is not valid JSON, storing as string");
            }

            // Create the request to put item in DynamoDB
            var request = new PutItemRequest
            {
                TableName = _tableName,
                Item = messageData
            };

            // Write to DynamoDB
            await _dynamoDbClient.PutItemAsync(request);
            _logger.LogInformation($"Successfully wrote message {record.MessageId} to DynamoDB table {_tableName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error writing message {record.MessageId} to DynamoDB");
            throw; // Re-throw to handle in the calling function
        }
    }

    /// <summary>
    /// Helper method to extract JSON elements recursively
    /// </summary>
    private void ExtractJsonElements(JsonElement element, Dictionary<string, AttributeValue> attributes, string prefix)
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
                attributes.Add(prefix, new AttributeValue { S = arrayJson });
                break;
            case JsonValueKind.String:
                attributes.Add(prefix, new AttributeValue { S = element.GetString() ?? string.Empty });
                break;
            case JsonValueKind.Number:
                attributes.Add(prefix, new AttributeValue { N = element.GetRawText() });
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                attributes.Add(prefix, new AttributeValue { BOOL = element.GetBoolean() });
                break;
            case JsonValueKind.Null:
                attributes.Add(prefix, new AttributeValue { NULL = true });
                break;
            default:
                attributes.Add(prefix, new AttributeValue { S = element.GetRawText() });
                break;
        }
    }
}
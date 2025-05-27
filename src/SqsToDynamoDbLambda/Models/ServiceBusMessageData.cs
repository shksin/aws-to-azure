using System.Text.Json.Serialization;

namespace SqsToDynamoDbLambda.Models;

/// <summary>
/// Model representing an Azure Service Bus message to be stored in Cosmos DB
/// </summary>
public class ServiceBusMessageData
{
    /// <summary>
    /// The unique identifier for the message
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The message body
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Content type of the message
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// Correlation ID for the message
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Message ID from Service Bus
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was received by the Function App
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Optional message properties (as a JSON string)
    /// </summary>
    [JsonPropertyName("userProperties")]
    public string? UserProperties { get; set; }
}
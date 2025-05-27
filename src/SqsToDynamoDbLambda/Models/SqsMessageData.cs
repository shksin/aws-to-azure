using System.Text.Json.Serialization;

namespace SqsToDynamoDbLambda.Models;

/// <summary>
/// Model representing an SQS message to be stored in DynamoDB
/// </summary>
public class SqsMessageData
{
    /// <summary>
    /// The unique identifier for the message
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The receipt handle is a token used to delete the message from the queue
    /// </summary>
    [JsonPropertyName("receiptHandle")]
    public string ReceiptHandle { get; set; } = string.Empty;

    /// <summary>
    /// The message body
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// An MD5 digest of the message body
    /// </summary>
    [JsonPropertyName("md5OfBody")]
    public string Md5OfBody { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was received by the Lambda function
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Optional message attributes (as a JSON string)
    /// </summary>
    [JsonPropertyName("messageAttributes")]
    public string? MessageAttributes { get; set; }
}
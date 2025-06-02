using Amazon.Lambda.SQSEvents;

namespace SqsToDynamoDbLambda.Services;

/// <summary>
/// Interface for DynamoDB operations
/// </summary>
public interface IDynamoDbService
{
    /// <summary>
    /// Writes a message from SQS to DynamoDB
    /// </summary>
    /// <param name="record">The SQS event record</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task WriteMessageToDynamoDbAsync(SQSEvent.SQSMessage record);
}
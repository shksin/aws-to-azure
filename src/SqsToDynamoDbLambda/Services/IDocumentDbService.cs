using Azure.Messaging.ServiceBus;

namespace SqsToDynamoDbLambda.Services;

/// <summary>
/// Interface for Cosmos DB operations
/// </summary>
public interface IDocumentDbService
{
    /// <summary>
    /// Writes a message from Service Bus to Cosmos DB
    /// </summary>
    /// <param name="message">The Service Bus message</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task WriteMessageToCosmosDbAsync(ServiceBusReceivedMessage message);
}
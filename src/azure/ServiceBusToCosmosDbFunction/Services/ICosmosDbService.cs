using Azure.Messaging.ServiceBus;

namespace ServiceBusToCosmosDbFunction.Services;

/// <summary>
/// Interface for Cosmos DB operations
/// </summary>
public interface ICosmosDbService
{
    /// <summary>
    /// Writes a message from Service Bus to Cosmos DB
    /// </summary>
    /// <param name="message">The Service Bus message</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task WriteMessageToCosmosDbAsync(ServiceBusReceivedMessage message);
}
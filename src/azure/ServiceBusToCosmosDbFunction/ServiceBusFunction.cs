using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceBusToCosmosDbFunction.Services;

namespace ServiceBusToCosmosDbFunction;

/// <summary>
/// Azure Function that processes messages from Service Bus and stores them in Cosmos DB
/// </summary>
public class ServiceBusFunction
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<ServiceBusFunction> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public ServiceBusFunction(ICosmosDbService cosmosDbService, ILogger<ServiceBusFunction> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    /// <summary>
    /// Azure Function handler method that processes Service Bus messages
    /// </summary>
    /// <param name="message">The Service Bus message</param>
    /// <param name="messageActions">Service Bus message actions for completion/abandonment</param>
    /// <returns>Async Task</returns>
    [Function("ServiceBusProcessor")]
    public async Task Run(
        [ServiceBusTrigger("messagequeue", Connection = "ServiceBusConnectionString")] 
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        if (message == null)
        {
            _logger.LogWarning("Received null Service Bus message");
            return;
        }

        _logger.LogInformation($"Processing message {message.MessageId}");

        try
        {
            // Process the message and write to Cosmos DB
            await _cosmosDbService.WriteMessageToCosmosDbAsync(message);

            _logger.LogInformation($"Successfully processed message {message.MessageId}");
            
            // Complete the message to remove it from the queue
            await messageActions.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            // Log the error and abandon the message so it can be retried
            _logger.LogError(ex, $"Error processing message {message.MessageId}");
            await messageActions.AbandonMessageAsync(message);
        }
    }
}
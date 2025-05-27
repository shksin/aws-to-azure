using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqsToDynamoDbLambda.Services;
using System;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(SqsToDynamoDbLambda.Startup))]

namespace SqsToDynamoDbLambda;

/// <summary>
/// Startup class for Azure Functions
/// </summary>
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        // Register services for dependency injection
        builder.Services.AddSingleton<IDocumentDbService, CosmosDbService>();
    }
}

/// <summary>
/// Azure Function that processes messages from Service Bus Queue and stores them in Cosmos DB
/// </summary>
public class Function
{
    private readonly IDocumentDbService _documentDbService;
    private readonly ILogger<Function> _logger;

    /// <summary>
    /// Constructor for use with dependency injection
    /// </summary>
    public Function(IDocumentDbService documentDbService, ILogger<Function> logger)
    {
        _documentDbService = documentDbService;
        _logger = logger;
    }

    /// <summary>
    /// Azure Function handler method that processes Service Bus queue messages
    /// </summary>
    /// <param name="message">The Service Bus message</param>
    /// <param name="log">Function logger</param>
    /// <returns>Async Task</returns>
    [FunctionName("ServiceBusQueueTrigger")]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message,
        ILogger log)
    {
        if (message == null)
        {
            log.LogWarning("No Service Bus message received");
            _logger.LogWarning("No Service Bus message received");
            return;
        }

        try
        {
            log.LogInformation($"Processing message {message.MessageId}");
            _logger.LogInformation($"Processing message {message.MessageId}");

            // Process the message and write to Cosmos DB
            await _documentDbService.WriteMessageToCosmosDbAsync(message);

            log.LogInformation($"Successfully processed message {message.MessageId}");
            _logger.LogInformation($"Successfully processed message {message.MessageId}");
        }
        catch (Exception ex)
        {
            // Log the error
            log.LogError($"Error processing message {message.MessageId}: {ex.Message}");
            _logger.LogError(ex, $"Error processing message {message.MessageId}");
            throw; // Rethrow to ensure Azure Functions can handle the error properly
        }
    }
}
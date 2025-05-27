using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqsToDynamoDbLambda.Services;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SqsToDynamoDbLambda;

/// <summary>
/// Lambda function that processes messages from SQS and stores them in DynamoDB
/// </summary>
public class Function
{
    private readonly IDynamoDbService _dynamoDbService;
    private readonly ILogger<Function> _logger;

    /// <summary>
    /// Default constructor used by AWS Lambda
    /// </summary>
    public Function()
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        _dynamoDbService = serviceProvider.GetRequiredService<IDynamoDbService>();
        _logger = serviceProvider.GetRequiredService<ILogger<Function>>();
    }

    /// <summary>
    /// Constructor for use with dependency injection (used in testing)
    /// </summary>
    public Function(IDynamoDbService dynamoDbService, ILogger<Function> logger)
    {
        _dynamoDbService = dynamoDbService;
        _logger = logger;
    }

    /// <summary>
    /// Configure the dependency injection container
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
        });

        // Add DynamoDB service
        services.AddSingleton<IDynamoDbService, DynamoDbService>();
    }

    /// <summary>
    /// Lambda handler method that processes SQS events
    /// </summary>
    /// <param name="sqsEvent">The SQS event containing one or more messages</param>
    /// <param name="context">The Lambda context</param>
    /// <returns>Async Task</returns>
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        if (sqsEvent.Records == null || !sqsEvent.Records.Any())
        {
            context.Logger.LogWarning("No SQS messages received");
            _logger.LogWarning("No SQS messages received");
            return;
        }

        context.Logger.LogInformation($"Beginning to process {sqsEvent.Records.Count} records");
        _logger.LogInformation($"Beginning to process {sqsEvent.Records.Count} records");

        foreach (var record in sqsEvent.Records)
        {
            try
            {
                context.Logger.LogInformation($"Processing message {record.MessageId}");
                _logger.LogInformation($"Processing message {record.MessageId}");

                // Process the message and write to DynamoDB
                await _dynamoDbService.WriteMessageToDynamoDbAsync(record);

                context.Logger.LogInformation($"Successfully processed message {record.MessageId}");
                _logger.LogInformation($"Successfully processed message {record.MessageId}");
            }
            catch (Exception ex)
            {
                // Log the error but don't throw to ensure other messages are processed
                context.Logger.LogError($"Error processing message {record.MessageId}: {ex.Message}");
                _logger.LogError(ex, $"Error processing message {record.MessageId}");
            }
        }

        context.Logger.LogInformation("Completed processing of all records");
        _logger.LogInformation("Completed processing of all records");
    }
}
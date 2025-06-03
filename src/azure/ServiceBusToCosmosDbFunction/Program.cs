using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ServiceBusToCosmosDbFunction.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Add configuration
        services.AddSingleton<IConfiguration>(context.Configuration);
        
        // Add Cosmos DB service
        services.AddSingleton<ICosmosDbService, CosmosDbService>();
    })
    .Build();

host.Run();
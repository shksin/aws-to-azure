# Azure Bicep Deployment Guide for ServiceBusToCosmosDbFunction

This guide explains how to deploy the Service Bus to Cosmos DB Azure Function using Azure Bicep.

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed and configured
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local) installed (optional, for local development)

Make sure you're logged into Azure CLI:
```bash
az login
```

## Deployment Steps

### 1. Build and Package the Azure Function

Navigate to the project directory:
```bash
cd src/azure/ServiceBusToCosmosDbFunction
```

Build the project:
```bash
dotnet build -c Release
```

### 2. Deploy Using Bicep

Run the deployment script:
```bash
cd infra-bicep
./deploy.sh --resource-group my-resource-group --location eastus
```

### 3. Alternative Manual Deployment

You can also deploy manually:

1. **Create a resource group**:
   ```bash
   az group create --name my-resource-group --location eastus
   ```

2. **Deploy the Bicep template**:
   ```bash
   az deployment group create \
     --resource-group my-resource-group \
     --template-file main.bicep \
     --name servicebus-cosmosdb-deployment
   ```

3. **Build and deploy the function code**:
   ```bash
   cd ../
   dotnet publish -c Release -o ./publish
   cd publish
   zip -r ../function-app.zip .
   cd ..
   
   # Get function app name from deployment
   FUNCTION_APP_NAME=$(az deployment group show \
     --resource-group my-resource-group \
     --name servicebus-cosmosdb-deployment \
     --query "properties.outputs.functionAppName.value" \
     --output tsv)
   
   # Deploy function code
   az functionapp deployment source config-zip \
     --resource-group my-resource-group \
     --name $FUNCTION_APP_NAME \
     --src function-app.zip
   ```

## Parameters

You can customize the deployment by modifying the Bicep template parameters:

- `namePrefix`: Prefix for all resource names (default: sbcosmos)
- `location`: Azure region for resources (default: resource group location)
- `serviceBusNamespaceName`: Name of the Service Bus namespace
- `queueName`: Name of the Service Bus queue (default: messagequeue)
- `cosmosDbAccountName`: Name of the Cosmos DB account
- `cosmosDbDatabaseName`: Name of the Cosmos DB database (default: MessageDatabase)
- `cosmosDbContainerName`: Name of the Cosmos DB container (default: ServiceBusMessages)
- `functionAppName`: Name of the Function App

## Resources Created

1. **Azure Function App**: Processes Service Bus messages
2. **Service Bus Namespace and Queue**: Receives messages
3. **Cosmos DB Account, Database, and Container**: Stores message data
4. **Storage Account**: Required for Azure Functions
5. **Application Insights**: For monitoring and logging
6. **App Service Plan**: Consumption plan for serverless execution

## Testing the Deployment

After deployment, you can test by sending messages to the Service Bus queue:

1. **Get the Service Bus connection string** from the Azure portal or CLI
2. **Send a test message** using Azure Service Bus Explorer or Azure CLI:
   ```bash
   az servicebus queue send \
     --resource-group my-resource-group \
     --namespace-name <service-bus-namespace> \
     --queue-name messagequeue \
     --body '{"id": 123, "name": "Test Message", "value": 42.5}'
   ```

3. **Check Cosmos DB** to verify the message was processed and stored

## Environment Variables

The Function App is configured with the following environment variables:

- `ServiceBusConnectionString`: Connection string for Service Bus
- `CosmosDbConnectionString`: Connection string for Cosmos DB  
- `CosmosDbDatabaseName`: Name of the Cosmos DB database
- `CosmosDbContainerName`: Name of the Cosmos DB container

## Local Development

For local development:

1. Copy `local.settings.json.example` to `local.settings.json`
2. Fill in the connection strings from your Azure resources
3. Run the function locally:
   ```bash
   func start
   ```

## Cleanup

To delete all resources:
```bash
az group delete --name my-resource-group --yes
```
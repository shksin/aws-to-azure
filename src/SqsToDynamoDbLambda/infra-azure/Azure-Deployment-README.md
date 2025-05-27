# Azure Deployment Guide for ServiceBusToCosmosDbFunction

This guide explains how to deploy the Azure Function that processes Service Bus Queue messages and stores them in a Cosmos DB container.

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed and configured
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local) installed

## Deployment Steps

### 1. Create Azure Resources

First, create the necessary Azure resources:

```bash
# Login to Azure
az login

# Set default subscription (if needed)
az account set --subscription <your-subscription-id>

# Create resource group
az group create --name message-processing-rg --location eastus

# Create Storage Account for Function App
az storage account create --name msgprocstore --location eastus --resource-group message-processing-rg --sku Standard_LRS

# Create Service Bus namespace
az servicebus namespace create --name msgproc-servicebus --resource-group message-processing-rg --location eastus --sku Basic

# Create Service Bus queue
az servicebus queue create --name messagequeue --namespace-name msgproc-servicebus --resource-group message-processing-rg

# Create Cosmos DB account
az cosmosdb create --name msgproc-cosmos --resource-group message-processing-rg --locations regionName=eastus

# Create Cosmos DB database
az cosmosdb sql database create --account-name msgproc-cosmos --resource-group message-processing-rg --name MessageDatabase

# Create Cosmos DB container with partition key
az cosmosdb sql container create --account-name msgproc-cosmos --resource-group message-processing-rg --database-name MessageDatabase --name Messages --partition-key-path "/id" --throughput 400
```

### 2. Create Function App

Create an Azure Function App:

```bash
# Create Function App
az functionapp create --name msgproc-function --storage-account msgprocstore --consumption-plan-location eastus --resource-group message-processing-rg --runtime dotnet --functions-version 4 --os-type Windows
```

### 3. Get Connection Information

Retrieve connection strings for configuration:

```bash
# Get the Service Bus connection string
SERVICE_BUS_CONNECTION=$(az servicebus namespace authorization-rule keys list --resource-group message-processing-rg --namespace-name msgproc-servicebus --name RootManageSharedAccessKey --query primaryConnectionString --output tsv)

# Get the Cosmos DB connection string
COSMOS_DB_CONNECTION=$(az cosmosdb keys list --name msgproc-cosmos --resource-group message-processing-rg --type connection-strings --query connectionStrings[0].connectionString --output tsv)
```

### 4. Configure Application Settings

Configure the Function App settings:

```bash
# Set application settings
az functionapp config appsettings set --name msgproc-function --resource-group message-processing-rg --settings "ServiceBusConnectionString=$SERVICE_BUS_CONNECTION" "ServiceBusQueueName=messagequeue" "CosmosDbConnectionString=$COSMOS_DB_CONNECTION" "CosmosDbDatabaseName=MessageDatabase" "CosmosDbContainerName=Messages"
```

### 5. Build and Deploy the Function App

```bash
# Navigate to the project directory
cd src/SqsToDynamoDbLambda

# Build the function
dotnet build -c Release

# Publish the function
dotnet publish -c Release -o ./publish

# Deploy to Azure
func azure functionapp publish msgproc-function --csharp
```

## Testing the Deployment

After deployment, you can test the setup by sending a message to the Service Bus queue:

```bash
# Send a test message 
az servicebus queue send --resource-group message-processing-rg --namespace-name msgproc-servicebus --queue-name messagequeue --body '{"id": 123, "name": "Test Message", "value": 42.5}'
```

Then check the Cosmos DB container to confirm the message was processed:

```bash
# Query items in container (requires cosmos extension)
az extension add --name cosmosdb-preview
az cosmosdb sql query --account-name msgproc-cosmos --resource-group message-processing-rg --database-name MessageDatabase --container-name Messages --query "SELECT * FROM c"
```

## Monitoring

View logs and monitor your function:

```bash
# View function logs
az functionapp log tail --name msgproc-function --resource-group message-processing-rg
```

## Cleanup

To delete all resources created:

```bash
az group delete --name message-processing-rg --yes
```
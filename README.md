# aws-to-azure

This repository contains AWS and Azure integration examples.

## Service Bus to Cosmos DB Function App

A .NET 8 Azure Function App that reads messages from an Azure Service Bus queue and stores them in a Cosmos DB container.

### Features

- Processes Service Bus messages automatically when they arrive in the configured queue
- Parses and stores message data in Cosmos DB
- Handles both plain text and JSON message bodies
- Provides detailed logging
- Uses dependency injection for better testability

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) configured with appropriate credentials
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local) (optional, for local development and deployment)

### Project Structure

```
src/
  └─ SqsToDynamoDbLambda/     # Function App source code (original name kept for compatibility)
     ├─ Models/               # Data models
     ├─ Services/             # Service implementations
     ├─ Function.cs           # Azure Function
     ├─ infra-azure/          # Azure infrastructure
     │  ├─ azuredeploy.json      # ARM template
     │  ├─ deploy.sh             # Deployment script
     │  └─ azuredeploy.parameters.json # ARM template parameters
     └─ SqsToDynamoDbLambda.csproj # Project file
```

### Deployment

#### Option 1: Manual Deployment

1. **Install the Azure Functions Core Tools** (if not already installed):
   ```
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

2. **Deploy the Function App from the project directory**:
   ```
   cd src/SqsToDynamoDbLambda
   func azure functionapp publish your-function-app-name
   ```
   
3. **Configure the Function App settings**:
   - In the Azure Portal, navigate to the Function App
   - Add the required application settings (connection strings, etc.)

#### Option 2: ARM Template Deployment

Deploy all resources (Function App, Service Bus, Cosmos DB, and associated configurations) using ARM templates:

1. **Navigate to the project directory**:
   ```
   cd src/SqsToDynamoDbLambda
   ```

2. **Run the deployment script**:
   ```
   cd infra-azure
   ./deploy.sh --resource-group your-resource-group
   ```

For more detailed instructions on Azure deployment, see [Azure-Deployment-README.md](src/SqsToDynamoDbLambda/infra-azure/Azure-Deployment-README.md) in the infra-azure folder.

### Application Settings

- `ServiceBusConnectionString`: Connection string for Azure Service Bus
- `ServiceBusQueueName`: Name of the Service Bus queue (default: "messagequeue")
- `CosmosDbConnectionString`: Connection string for Azure Cosmos DB
- `CosmosDbDatabaseName`: Name of the Cosmos DB database (default: "MessageDatabase")
- `CosmosDbContainerName`: Name of the Cosmos DB container (default: "Messages")

### Resources Created

1. **Function App**: Processes Service Bus messages
2. **Service Bus Namespace and Queue**: Provides message queue functionality
3. **Cosmos DB Account, Database, and Container**: Stores message data
   - Required attributes:
     - id (String): Primary key/partition key

### Local Testing

Run a local test with a sample Service Bus message event:

```bash
cd src/SqsToDynamoDbLambda
func start --verbose
```

In another terminal, you can test the function with the provided test event:
```bash
# Using cURL or another tool to test the function endpoint
```

Example `test-azure-event.json`:
```json
{
  "message": {
    "messageId": "test-message-01",
    "body": "{\"id\": 123, \"name\": \"Test Message\", \"value\": 42.5}",
    "contentType": "application/json",
    "applicationProperties": {
      "source": "test",
      "priority": "high"
    }
  }
}
```

# aws-to-azure

This repository contains AWS and Azure integration examples that demonstrate equivalent messaging and storage patterns.

## SQS to DynamoDB Lambda Function (AWS)

A .NET 8 AWS Lambda function that reads messages from an SQS queue and stores them in a DynamoDB table.

### Features

- Processes SQS events automatically when messages arrive in the configured queue
- Parses and stores message data in DynamoDB
- Handles both plain text and JSON message bodies
- Provides detailed logging
- Uses dependency injection for better testability

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [AWS CLI](https://aws.amazon.com/cli/) configured with appropriate credentials
- [AWS Lambda .NET Global Tool](https://github.com/aws/aws-lambda-dotnet) (optional, for deployment)

### Project Structure

```
src/
  └─ SqsToDynamoDbLambda/     # Lambda function source code
     ├─ Models/               # Data models
     ├─ Services/             # Service implementations
     ├─ Function.cs           # Lambda function handler
     ├─ infra-cf/             # CloudFormation infrastructure
     │  ├─ cloudformation.yaml   # CloudFormation template
     │  ├─ deploy.sh             # Deployment script
     │  └─ cloudformation-parameters.json # CloudFormation parameters
     └─ SqsToDynamoDbLambda.csproj # Project file
```

### Deployment

#### Option 1: Manual Deployment

1. **Install the AWS Lambda .NET Global Tool** (if not already installed):
   ```
   dotnet tool install -g Amazon.Lambda.Tools
   ```

2. **Deploy the Lambda function from the project directory**:
   ```
   cd src/SqsToDynamoDbLambda
   dotnet lambda deploy-function
   ```
   
3. **Configure the Lambda function trigger**:
   - In the AWS Console, navigate to the Lambda function
   - Add an SQS trigger and select the desired queue
   - Configure batch size and other settings as needed

#### Option 2: CloudFormation Deployment

Deploy all resources (Lambda, SQS queue, DynamoDB table, and IAM roles) using CloudFormation:

1. **Navigate to the project directory**:
   ```
   cd src/SqsToDynamoDbLambda
   ```

2. **Run the deployment script**:
   ```
   cd src/SqsToDynamoDbLambda/infra-cf
   ./deploy.sh --s3-bucket your-bucket-name
   ```

For more detailed instructions on CloudFormation deployment, see [CloudFormation-README.md](src/SqsToDynamoDbLambda/infra-cf/CloudFormation-README.md) in the infra-cf folder.

### Environment Variables

- `DYNAMODB_TABLE_NAME`: Name of the DynamoDB table (default: "SqsMessages")

### Resources Created

1. **Lambda Function**: Processes SQS messages
2. **DynamoDB Table**: Stores message data
   - Required attributes:
     - MessageId (String): Primary key

### Local Testing

Run a local test with a sample SQS event:

```bash
cd src/SqsToDynamoDbLambda
dotnet lambda invoke-function -p ./test-event.json
```

Example `test-event.json`:
```json
{
  "Records": [
    {
      "messageId": "test-message-01",
      "receiptHandle": "receipt-handle",
      "body": "{\"id\": 123, \"name\": \"Test Message\", \"value\": 42.5}",
      "md5OfBody": "md5-hash",
      "eventSource": "aws:sqs",
      "eventSourceARN": "arn:aws:sqs:region:account:queue",
      "awsRegion": "us-east-1"
    }
  ]
}
```

## Service Bus to Cosmos DB Azure Function (Azure)

A .NET 8 Azure Function that reads messages from a Service Bus queue and stores them in a Cosmos DB container. This is the Azure equivalent of the AWS SQS to DynamoDB Lambda function.

### Features

- Processes Service Bus messages automatically when messages arrive in the configured queue
- Parses and stores message data in Cosmos DB
- Handles both plain text and JSON message bodies
- Provides detailed logging and error handling
- Uses dependency injection for better testability
- Maintains the same data structure and JSON parsing capabilities as the AWS version

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) configured with appropriate credentials
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local) (optional, for local development)

### Project Structure

```
src/
  └─ azure/
     └─ ServiceBusToCosmosDbFunction/  # Azure Function source code
        ├─ Services/                   # Service implementations
        ├─ ServiceBusFunction.cs       # Function handler
        ├─ Program.cs                  # Startup configuration
        ├─ infra-bicep/               # Azure Bicep infrastructure
        │  ├─ main.bicep              # Bicep template
        │  ├─ deploy.sh               # Deployment script
        │  └─ Bicep-README.md         # Bicep deployment guide
        ├─ host.json                  # Function host configuration
        ├─ local.settings.json        # Local development settings
        └─ ServiceBusToCosmosDbFunction.csproj # Project file
```

### Deployment

#### Option 1: Bicep Deployment (Recommended)

Deploy all resources (Function App, Service Bus, Cosmos DB, and other required resources) using Azure Bicep:

1. **Navigate to the project directory**:
   ```
   cd src/azure/ServiceBusToCosmosDbFunction
   ```

2. **Run the deployment script**:
   ```
   cd infra-bicep
   ./deploy.sh --resource-group my-resource-group --location eastus
   ```

For more detailed instructions on Bicep deployment, see [Bicep-README.md](src/azure/ServiceBusToCosmosDbFunction/infra-bicep/Bicep-README.md) in the infra-bicep folder.

#### Option 2: Manual Deployment

1. **Install Azure Functions Core Tools** (if not already installed):
   ```
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

2. **Deploy the Azure Function from the project directory**:
   ```
   cd src/azure/ServiceBusToCosmosDbFunction
   func azure functionapp publish <your-function-app-name>
   ```

3. **Configure the Function App settings**:
   - In the Azure Portal, navigate to the Function App
   - Add the required application settings (Service Bus connection string, Cosmos DB connection string, etc.)

### Environment Variables

- `ServiceBusConnectionString`: Connection string for Service Bus namespace
- `CosmosDbConnectionString`: Connection string for Cosmos DB account
- `CosmosDbDatabaseName`: Name of the Cosmos DB database (default: "MessageDatabase")
- `CosmosDbContainerName`: Name of the Cosmos DB container (default: "ServiceBusMessages")

### Resources Created

1. **Azure Function App**: Processes Service Bus messages
2. **Service Bus Namespace and Queue**: Receives messages
3. **Cosmos DB Account, Database, and Container**: Stores message data
   - Partition Key: `/MessageId`
   - Serverless mode for cost optimization
4. **Storage Account**: Required for Azure Functions
5. **Application Insights**: For monitoring and logging

### Local Testing

Run a local test with Azure Functions Core Tools:

```bash
cd src/azure/ServiceBusToCosmosDbFunction
func start
```

Example message to send to Service Bus:
```json
{
  "id": 123,
  "name": "Test Message",
  "value": 42.5
}
```

### AWS vs Azure Service Mapping

| AWS Service | Azure Equivalent | Purpose |
|-------------|------------------|---------|
| AWS Lambda | Azure Functions | Serverless compute |
| Amazon SQS | Azure Service Bus | Message queuing |
| Amazon DynamoDB | Azure Cosmos DB | NoSQL database |
| CloudWatch | Application Insights | Monitoring and logging |
| CloudFormation | Azure Bicep/ARM | Infrastructure as Code |

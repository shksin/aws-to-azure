# aws-to-azure

This repository contains AWS and Azure integration examples.

## SQS to DynamoDB Lambda Function

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
     └─ SqsToDynamoDbLambda.csproj # Project file
```

### Deployment

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

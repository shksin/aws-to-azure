# CloudFormation Deployment Guide for SqsToDynamoDbLambda

This guide explains how to deploy the SQS to DynamoDB Lambda function using AWS CloudFormation.

## Prerequisites

- [AWS CLI](https://aws.amazon.com/cli/) installed and configured
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- [AWS Lambda .NET Global Tool](https://github.com/aws/aws-lambda-dotnet) installed

## Deployment Steps

### 1. Build and Package the Lambda Function

```bash
# Navigate to the project directory
cd src/SqsToDynamoDbLambda

# Build the function
dotnet build -c Release

# Package the function
dotnet lambda package -c Release -o ./infra-cf/lambda-function.zip
```

### 2. Create an S3 Bucket for Deployment (if needed)

```bash
aws s3 mb s3://your-deployment-bucket --region your-region
```

### 3. Upload the Lambda Package to S3

```bash
aws s3 cp ./infra-cf/lambda-function.zip s3://your-deployment-bucket/
```

### 4. Update the CloudFormation Template

Open `infra-cf/cloudformation.yaml` and replace `DEPLOYMENT_BUCKET_NAME_TO_BE_REPLACED` with your actual S3 bucket name.

### 5. Deploy the CloudFormation Stack

```bash
aws cloudformation deploy \
  --template-file infra-cf/cloudformation.yaml \
  --stack-name sqs-to-dynamodb-stack \
  --capabilities CAPABILITY_IAM \
  --parameter-overrides \
      LambdaFunctionName=SqsToDynamoDbFunction \
      DynamoDBTableName=SqsMessages \
      SQSQueueName=SqsMessageQueue
```

## Parameters

You can customize the deployment by overriding the following parameters:

- `LambdaFunctionName`: Name of the Lambda function (default: SqsToDynamoDbFunction)
- `DynamoDBTableName`: Name of the DynamoDB table (default: SqsMessages)
- `SQSQueueName`: Name of the SQS queue (default: SqsMessageQueue)
- `LambdaMemorySize`: Memory size for Lambda in MB (default: 256)
- `LambdaTimeout`: Timeout for Lambda in seconds (default: 30)

Example with custom parameters:

```bash
aws cloudformation deploy \
  --template-file infra-cf/cloudformation.yaml \
  --stack-name sqs-to-dynamodb-stack \
  --capabilities CAPABILITY_IAM \
  --parameter-overrides \
      LambdaFunctionName=MyCustomLambda \
      DynamoDBTableName=MyCustomTable \
      SQSQueueName=MyCustomQueue \
      LambdaMemorySize=512 \
      LambdaTimeout=60
```

## Testing the Deployment

After deployment, you can test the setup by sending a message to the SQS queue:

```bash
# Get the queue URL from the CloudFormation stack outputs
QUEUE_URL=$(aws cloudformation describe-stacks --stack-name sqs-to-dynamodb-stack --query "Stacks[0].Outputs[?OutputKey=='SqsQueueUrl'].OutputValue" --output text)

# Send a test message
aws sqs send-message --queue-url $QUEUE_URL --message-body '{"id": 123, "name": "Test Message", "value": 42.5}'
```

Then check the DynamoDB table to confirm the message was processed:

```bash
TABLE_NAME=$(aws cloudformation describe-stacks --stack-name sqs-to-dynamodb-stack --query "Stacks[0].Outputs[?OutputKey=='DynamoDBTableName'].OutputValue" --output text)

aws dynamodb scan --table-name $TABLE_NAME
```

## Cleanup

To delete all resources created by the CloudFormation stack:

```bash
aws cloudformation delete-stack --stack-name sqs-to-dynamodb-stack
```
#!/bin/bash
# Script to deploy SqsToDynamoDbLambda using CloudFormation

# Set defaults
STACK_NAME="sqs-to-dynamodb-stack"
S3_BUCKET=""
REGION="us-east-1"
PARAM_FILE="cloudformation-parameters.json"
SCRIPT_DIR=$(dirname "$0")
PROJECT_DIR=".."

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --stack-name)
      STACK_NAME="$2"
      shift 2
      ;;
    --s3-bucket)
      S3_BUCKET="$2"
      shift 2
      ;;
    --region)
      REGION="$2"
      shift 2
      ;;
    --param-file)
      PARAM_FILE="$2"
      shift 2
      ;;
    --help)
      echo "Usage: $0 --s3-bucket BUCKET_NAME [options]"
      echo ""
      echo "Options:"
      echo "  --stack-name NAME    CloudFormation stack name (default: sqs-to-dynamodb-stack)"
      echo "  --s3-bucket BUCKET   S3 bucket for lambda deployment package (required)"
      echo "  --region REGION      AWS region (default: us-east-1)"
      echo "  --param-file FILE    Parameter file path (default: cloudformation-parameters.json)"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

# Check required parameters
if [ -z "$S3_BUCKET" ]; then
  echo "Error: S3 bucket name is required. Use --s3-bucket to specify it."
  echo "Run '$0 --help' for usage information."
  exit 1
fi

echo "=== Building and packaging Lambda function ==="
cd $PROJECT_DIR
dotnet build -c Release
if [ $? -ne 0 ]; then
  echo "Build failed"
  exit 1
fi

dotnet lambda package -c Release -o $SCRIPT_DIR/lambda-function.zip
if [ $? -ne 0 ]; then
  echo "Packaging failed"
  exit 1
fi

echo "=== Uploading Lambda package to S3 ==="
cd $SCRIPT_DIR
aws s3 cp ./lambda-function.zip s3://$S3_BUCKET/ --region $REGION
if [ $? -ne 0 ]; then
  echo "Upload to S3 failed"
  exit 1
fi

echo "=== Creating temporary CloudFormation template with S3 bucket name ==="
sed "s/DEPLOYMENT_BUCKET_NAME_TO_BE_REPLACED/$S3_BUCKET/g" $SCRIPT_DIR/cloudformation.yaml > $SCRIPT_DIR/cloudformation-deploy.yaml

echo "=== Deploying CloudFormation stack ==="
aws cloudformation deploy \
  --template-file $SCRIPT_DIR/cloudformation-deploy.yaml \
  --stack-name $STACK_NAME \
  --capabilities CAPABILITY_IAM \
  --parameter-overrides file://$SCRIPT_DIR/$PARAM_FILE \
  --region $REGION

if [ $? -ne 0 ]; then
  echo "CloudFormation deployment failed"
  rm $SCRIPT_DIR/cloudformation-deploy.yaml
  exit 1
fi

echo "=== Deployment completed successfully ==="
echo "Stack outputs:"
aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query "Stacks[0].Outputs" \
  --output table \
  --region $REGION

# Clean up
rm $SCRIPT_DIR/cloudformation-deploy.yaml
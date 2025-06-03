#!/bin/bash
# Script to deploy ServiceBusToCosmosDbFunction using Azure Bicep

# Set defaults
RESOURCE_GROUP=""
LOCATION="eastus"
DEPLOYMENT_NAME="servicebus-cosmosdb-deployment"
SCRIPT_DIR=$(dirname "$0")
PROJECT_DIR=".."

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --resource-group)
      RESOURCE_GROUP="$2"
      shift 2
      ;;
    --location)
      LOCATION="$2"
      shift 2
      ;;
    --deployment-name)
      DEPLOYMENT_NAME="$2"
      shift 2
      ;;
    --help)
      echo "Usage: $0 --resource-group RESOURCE_GROUP [options]"
      echo ""
      echo "Options:"
      echo "  --resource-group RG     Azure resource group name (required)"
      echo "  --location LOCATION     Azure region (default: eastus)"
      echo "  --deployment-name NAME  Deployment name (default: servicebus-cosmosdb-deployment)"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

# Check required parameters
if [ -z "$RESOURCE_GROUP" ]; then
  echo "Error: Resource group name is required. Use --resource-group to specify it."
  echo "Run '$0 --help' for usage information."
  exit 1
fi

echo "=== Building and packaging Azure Function ===="
cd $PROJECT_DIR
dotnet build -c Release
if [ $? -ne 0 ]; then
  echo "Build failed"
  exit 1
fi

dotnet publish -c Release -o ./publish
if [ $? -ne 0 ]; then
  echo "Publish failed"
  exit 1
fi

echo "=== Creating resource group if it doesn't exist ==="
az group create --name $RESOURCE_GROUP --location $LOCATION

echo "=== Deploying Bicep template ==="
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file $SCRIPT_DIR/main.bicep \
  --name $DEPLOYMENT_NAME

if [ $? -ne 0 ]; then
  echo "Deployment failed"
  exit 1
fi

echo "=== Deploying Function App code ==="
# Get the function app name from deployment output
FUNCTION_APP_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name $DEPLOYMENT_NAME \
  --query "properties.outputs.functionAppName.value" \
  --output tsv)

if [ -z "$FUNCTION_APP_NAME" ]; then
  echo "Failed to get Function App name from deployment"
  exit 1
fi

# Create zip package
cd ./publish
zip -r ../function-app.zip .
cd ..

# Deploy the function code
az functionapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP_NAME \
  --src function-app.zip

if [ $? -ne 0 ]; then
  echo "Function deployment failed"
  exit 1
fi

echo "=== Deployment completed successfully ==="
echo "Deployment outputs:"
az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name $DEPLOYMENT_NAME \
  --query "properties.outputs" \
  --output table

# Clean up
rm -f function-app.zip
rm -rf publish
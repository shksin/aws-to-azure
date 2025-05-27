#!/bin/bash
# Script to deploy ServiceBusToCosmosDbFunction using Azure ARM Templates

# Set defaults
RESOURCE_GROUP="message-processing-rg"
LOCATION="eastus"
PARAM_FILE="azuredeploy.parameters.json"
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
    --param-file)
      PARAM_FILE="$2"
      shift 2
      ;;
    --help)
      echo "Usage: $0 [options]"
      echo ""
      echo "Options:"
      echo "  --resource-group NAME  Azure resource group (default: message-processing-rg)"
      echo "  --location LOCATION    Azure region (default: eastus)"
      echo "  --param-file FILE      Parameter file path (default: azuredeploy.parameters.json)"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

# Login to Azure if not already logged in
az account show &> /dev/null
if [ $? -ne 0 ]; then
  echo "=== Logging in to Azure ==="
  az login
fi

# Create resource group if it doesn't exist
az group show --name $RESOURCE_GROUP &> /dev/null
if [ $? -ne 0 ]; then
  echo "=== Creating resource group $RESOURCE_GROUP ==="
  az group create --name $RESOURCE_GROUP --location $LOCATION
fi

echo "=== Building and packaging the Azure Function ==="
cd $SCRIPT_DIR/$PROJECT_DIR
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

echo "=== Deploying ARM template ==="
cd $SCRIPT_DIR
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file azuredeploy.json \
  --parameters @$PARAM_FILE

if [ $? -ne 0 ]; then
  echo "ARM template deployment failed"
  exit 1
fi

# Get the function app name from the deployment output
FUNCTION_APP_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name azuredeploy \
  --query properties.outputs.functionAppName.value \
  --output tsv)

echo "=== Deploying Azure Function $FUNCTION_APP_NAME ==="
cd $SCRIPT_DIR/$PROJECT_DIR
func azure functionapp publish $FUNCTION_APP_NAME --csharp

if [ $? -ne 0 ]; then
  echo "Function deployment failed"
  exit 1
fi

echo "=== Deployment completed successfully ==="
echo "Function app: $FUNCTION_APP_NAME"
echo "Resource group: $RESOURCE_GROUP"
echo ""
echo "You can monitor your function with:"
echo "az functionapp log tail --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP"
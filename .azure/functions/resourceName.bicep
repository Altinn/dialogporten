@description('Generates a unique resource name by appending a unique string (derived from the given subscription and resource group IDs), ensuring the total length does not exceed the limit. The name is always postfixed with the full 13-character unique string plus a dash.')
// Example:
// uniqueResourceName('my-resource', 50, subscription().id, resourceGroup().id) => 'my-resource-1234567890123'
@export()
func uniqueResourceName(name string, limit int, subscriptionId string, resourceGroupId string) string =>
  '${take(name, limit - 13 - 1)}-${uniqueString('${subscriptionId}${resourceGroupId}')}'

targetScope = 'resourceGroup'

@description('The deployment environment value used for FinOps tags')
@minLength(1)
param environment string

@description('Existing tags to merge FinOps tags into')
param existingTags object = {}

var finopsProduct = 'Dialogporten'
var repositoryUrl = 'https://github.com/altinn/dialogporten'

output tags object = union(existingTags, {
  finops_environment: environment
  finops_product: finopsProduct
  repository: repositoryUrl
})

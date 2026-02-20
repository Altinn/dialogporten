@description('Returns merged FinOps tags for the provided environment')
@export()
func baseTags(existingTags object, environment string) object => union(existingTags, {
  finops_environment: finopsEnvironment(environment)
  finops_product: 'Dialogporten'
  Environment: environment
  Product: 'Dialogporten'
  repository: 'https://github.com/altinn/dialogporten'
})

@description('Maps deployment environment to FinOps environment')
@export()
func finopsEnvironment(environment string) string => environment == 'test'
  ? 'dev'
  : (environment == 'staging'
    ? 'test'
    : (environment == 'yt01'
      ? 'test'
      : (environment == 'prod'
        ? 'prod'
        : environment)))

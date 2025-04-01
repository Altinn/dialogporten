# Graphql get dialogs

This directory holds a performance test with POST for `graphql`. The test file `graphql-search.js` is responsible for executing the performance test. It includes a list of end users (ssn) with pre-generated tokens and performs a POST request to the GraphQL endpoint with the payload `input: { party: ["urn:altinn:person:identifier-no:${identifier}"]}`. This test is designed to measure the performance of the GraphQL search functionality. 

## Prerequisites
- [K6 prerequisites](../../README.md#Prerequisites)

## Test files
The test file associated with this performance test is 
- `graphql-search.js`
>>Does graphql-queries on `party` and `search` for a random set of endusers and random `words`, some giving no hits. Equal to the nway af does searches.
- `graphqlRandomSearch.js`
>>Does random graphql-queries on a predefineds list of search-combinations, picking random values for the parameters
- `graphqlSearchWithThresholds.js`
>>Does the same as `graphql-search.js`, failing the test if response times exceed 500ms. Runs in the CI/CD yt01 pipeline

## Run test
### From cli
1. Navigate to the following directory:
```shell
cd tests/k6/tests/graphql/performance
```
2. Run the test using the following command. Replace `<test file>`, `<(test|staging|yt01)>`, `<vus>`, and `<duration>` with the desired values:
```shell
k6 run <test file> -e API_VERSION=v1 \
-e TOKEN_GENERATOR_USERNAME=<username> \
-e TOKEN_GENERATOR_PASSWORD=<passwd> \
-e API_ENVIRONMENT=<(test|staging|yt01)> \
--vus=<vus> --duration=<duration>
```
3. Refer to the k6 documentation for more information on usage.
### From GitHub Actions
To run the performance test using GitHub Actions, follow these steps:
1. Go to the [GitHub Actions](https://github.com/altinn/dialogporten/actions/workflows/dispatch-k6-performance.yml) page.
2. Select "Run workflow" and fill in the required parameters.
3. Tag the performance test with a descriptive name.

### GitHub Action with act
To run the performance test locally using GitHub Actions and act, perform the following steps:
1. [Install act](https://nektosact.com/installation/).
2. Navigate to the root of the repository.
3. Create a `.secrets` file that matches the GitHub secrets used. Example:
```file
TOKEN_GENERATOR_USERNAME:<username>
TOKEN_GENERATOR_PASSWORD:<passwd>
```
    Replace `<username>` and `<passwd>`, same as for generating tokens above. 
##### IMPORTANT: Ensure this file is added to .gitignore to prevent accidental commits of sensitive information. Never commit actual credentials to version control.
4. Run `act` using the command below. Replace `<vus>` and `<duration>` with the desired values:
```shell
act workflow_dispatch -j k6-performance -s GITHUB_TOKEN=`gh auth token` \
--container-architecture linux/amd64 --artifact-server-path $HOME/.act \ 
--input vus=<vus> --input duration=<duration> \ 
--input testSuitePath=tests/k6/tests/graphql/performance/graphql-search.js
```

## Test Results
Test results can be found in GitHub action run log, grafana and in App Insights.
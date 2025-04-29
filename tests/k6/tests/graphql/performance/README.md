# Graphql get dialogs

These performance tests execute POST requests against GraphQL endpoints using the scripts `graphql-search.js`, `graphqlRandomSearch.js`, and `graphqlSearchWithThresholds.js`

## Prerequisites
- [K6 prerequisites](../../README.md#Prerequisites)

## Test files

The test files associated with this performance test are:

| Filename                          | Description                                                                                                                       |
|:---------------------------------:|:----------------------------------------------------------------------------------------------------------------------------------|
| graphql-search.js                 | Performs GraphQL queries on `party` and `search` for a random set of end users and random words, some yielding no hits—mirroring AF. |
| graphqlRandomSearch.js            | Performs random GraphQL queries from a predefined list of search combinations, selecting parameters at random.                     |
| graphqlSearchWithThresholds.js    | Same as `graphql-search.js` but fails if response times exceed 500 ms; used in the CI/CD yt01 pipeline.                           |


## Run tests
### From cli
1. Navigate to the following directory:
```shell
cd tests/k6/tests/graphql/performance
```
2. Run the test using the following command. Replace `<test file>`, `<(test|staging|yt01)>`, `<vus>`, and `<duration>` with the desired values. If the environment variables (-e options) are set beforehand, they can be omitted:
```shell
k6 run <test-file> \
-e API_VERSION=v1 \
-e TOKEN_GENERATOR_USERNAME=<username> \
-e TOKEN_GENERATOR_PASSWORD=<passwd> \
-e API_ENVIRONMENT=<(test|staging|yt01)> \
--vus=<vus> \
--duration=<duration>
```
3. Refer to the k6 documentation for more information on usage.
### From GitHub Actions
To run the performance test using GitHub Actions, follow these steps:
1. Go to the [GitHub Actions](https://github.com/altinn/dialogporten/actions/workflows/dispatch-k6-performance.yml) page.
2. Select "Run workflow" and fill in the required parameters.
3. Tag the performance test with a descriptive name.

### GitHub Action with act
Running with act is primarily used for debugging GitHub workflows locally without committing to the repository. 
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
4. Run `act` using the command below. Replace `<path-to-testscript>`, `<vus>` and `<duration>` with the desired values:
```shell
act workflow_dispatch \
-j k6-performance \
-s GITHUB_TOKEN=`gh auth token` \
--container-architecture linux/amd64 \
--artifact-server-path $HOME/.act \
--input vus=<vus> \
--input duration=<duration> \
--input testSuitePath=<path-to-testscript>
```

## Test Results
The test results from tests run from GitHub Actions can be found in the GitHub Actions run log and [grafana](https://altinn-grafana-test-b2b8dpdkcvfuhfd3.eno.grafana.azure.com/d/eek8vrtzba8e8a/k6-prometheus-dialogporten?orgId=1&refresh=30s&var-DS_PROMETHEUS=k6tests-amw&var-namespace=dialogporten&var-testid=All&var-quantile_stat=p99&from=now-30m&to=now&var-adhoc_filter=url%7C%21~%7Ctesttools).
Otherwise, see the output from your CLI
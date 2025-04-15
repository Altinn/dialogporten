## Service Owner Performance Test

This performance test directory focuses on evaluating the GET and POST endpoints of the `serviceowner` API. The test files associated with this performance test are `create-dialog.js`, `create-remove-dialog.js`, `serviceowner-search.js`, and `purge-dialogs.js`. These files are designed to measure the performance and scalability of the API endpoints under different scenarios. By running these tests, you can gain insights into the system's response time, throughput, and resource utilization. Use the instructions below to execute the performance test and analyze the results.

### Prerequisites
Before running the performance test, make sure you have met the following prerequisites:
- [K6 prerequisites](../../../README.md#Prerequisites)

### Test Files
The test files associated with this performance test are 
- `create-dialog.js`
- `create-remove-dialog.js`
- `create-transmissions.js`
- `serviceowner-search.js`
- `purge-dialogs.js` (used for cleanup after test)

### Run Test
To run the performance test, follow the instructions below:

#### From CLI
1. Navigate to the following directory:
```shell
cd tests/k6/tests/serviceowner/performance
```
2. Run the test using the following command. Replace `<test-file>`, `<(test|staging|yt01)>`, `<vus>`, and `<duration>` with the desired values:
```shell
TOKEN_GENERATOR_USERNAME=<username> TOKEN_GENERATOR_PASSWORD=<passwd> \
k6 run <test-file> -e API_VERSION=v1 \
-e API_ENVIRONMENT=<(test|staging|yt01)> \
--vus=<vus> --duration=<duration>
```
3. Refer to the k6 documentation for more information on usage.

#### From GitHub Actions
To run the performance test using GitHub Actions, follow these steps:
1. Go to the [GitHub Actions](https://github.com/altinn/dialogporten/actions/workflows/dispatch-k6-performance.yml) page.
2. Select "Run workflow" and fill in the required parameters.
3. Tag the performance test with a descriptive name.

#### GitHub Action with act
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
4. Run `act` using the command below. Replace `<path-to-testscript>`, `<vus>`, `<duration>` and `<(personal|enterprise|both)>` with the desired values:
```shell
act workflow_dispatch -j k6-performance -s GITHUB_TOKEN=`gh auth token` \
--container-architecture linux/amd64 --artifact-server-path $HOME/.act \ 
--input vus=<vus> --input duration=<duration> \ 
--input testSuitePath=<path-to-testscript> 
```

Example of command:
```shell
act workflow_dispatch -j k6-performance -s GITHUB_TOKEN=`gh auth token` \
--container-architecture linux/amd64 --artifact-server-path $HOME/.act \ 
--input vus=10 --input duration=5m \ 
--input testSuitePath=tests/k6/tests/serviceowner/performance/create-dialog.js
```

#### Clean up
To clean up after the performance test, you can use the `purge-dialogs.js` test file. This file is specifically designed for cleanup purposes. It ensures that any resources created during the test, such as dialogs, are removed from the system.

To run the cleanup script, follow these steps:

1. Navigate to the following directory:
```shell
cd tests/k6/tests/serviceowner/performance
```

2. Run the cleanup script using the following command:
```shell
k6 run purge-dialogs.js -e API_VERSION=v1 \
-e API_ENVIRONMENT=<(test|staging|yt01)>
```

Replace `<(test|staging|yt01)>` with the appropriate environment where the test was executed.

This script will remove any dialogs created during the performance test, ensuring a clean state for future tests.

### Test Results
The test results can be found in the GitHub Actions run log and [grafana](https://altinn-grafana-test-b2b8dpdkcvfuhfd3.eno.grafana.azure.com/d/ccbb2351-2ae2-462f-ae0e-f2c893ad1028/k6-prometheus)

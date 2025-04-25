## Service Owner Performance Test

This performance test directory focuses on evaluating the GET and POST endpoints of the `serviceowner` API. The test files associated with this performance test are `create-dialog.js`, `create-remove-dialog.js`, `create-transmissions.js`, `serviceowner-search.js`, `createDialogBreakpoint.js`, `createDialogWithThresholds.js`, `createTransmissionsBreakpoint.js`, `createTransmissionsWithThresholds.js`, `serviceOwnerRandomSearch.js`, `serviceOwnerSearchBreakpoint.js`, `serviceOwnerSearchWithThreshold.js` and `purge-dialogs.js`. These files are designed to measure the performance and scalability of the API endpoints under different scenarios. By running these tests, you can gain insights into the system's response time, throughput, and resource utilization. Use the instructions below to execute the performance test and analyze the results.

### Prerequisites
Before running the performance test, make sure you have met the following prerequisites:
- [K6 prerequisites](../../../README.md#Prerequisites)

### Test Files
The test files associated with this performance test are:
|Filename|Description|
|:---:|:---:|
|create-dialog.js|Create dialogs|
|create-remove-dialog.js|Create a dialog and immediately removes it|
|createDialogBreakpoint.js|Gradually increases the load until limits are reached, then the test aborts|
|createDialogWithThresholds.js|Runs a test with response time thresholds. Used in CI/CD pipeline for yt01, and runs with only one VU for 30s|
|create-transmissions.js|First, creates a dialog and then creates a number of transmissions per dialog.|
|createTransmissionsBreakpoint.js|Gradually increases the load until limits are reached, then the test aborts|
|createTransmissionsWithThresholds.js|Runs the test with response time thresholds. Used in CI/CD pipeline for yt01, and runs with only one VU for 30s|
|serviceowner-search.js|Does a simple search on enduser and service resource and first GET dialogs, and then drills down into details, same way as [enduser search](../../enduser/performance/README.md#test-description)|
|serviceOwnerRandomSearch.js|The GET dialogs call uses random list of available url parameters, with some variations in values|
|serviceOwnerSearchBreakpoint.js|The purpose of this test is to gradually increase the load until certain thresholds are reached, indicating that the system breakpoint is reached|
|serviceOwnerSearchWithThresholds.js|Does the same as `serviceowner-search.js`, but with threshold-values for response times. Runs in the CI/CD workflow for yt01, and runs with one VU for 30s|
|purge-dialogs.js|Script that tries to clean up after a load test|


### Run Test
To run the performance test, follow the instructions below:

#### From CLI
1. Navigate to the following directory:
```shell
cd tests/k6/tests/serviceowner/performance
```
2. Run the test using the following command. Replace `<test-file>`, `<(test|staging|yt01)>`, `<vus>`, and `<duration>` with the desired values. If the environment variables (-e options) are set beforehand, they can be omitted:
```shell
k6 run <test-file> \
-e API_VERSION=v1 \
-e API_ENVIRONMENT=<(test|staging|yt01)> \
-e TOKEN_GENERATOR_USERNAME=<username> \
-e TOKEN_GENERATOR_PASSWORD=<passwd> \
--vus=<vus> \
--duration=<duration>
```
3. Refer to the k6 documentation for more information on usage.

#### From GitHub Actions
To run the performance tests using GitHub Actions, follow these steps:
1. Go to the [GitHub Actions](https://github.com/altinn/dialogporten/actions/workflows/dispatch-k6-performance.yml) page.
2. Select "Run workflow" and fill in the required parameters.
3. Tag the performance test with a descriptive name.

To run breakpoint-tests, follow the same flow from [this action](https://github.com/altinn/dialogporten/actions/workflows/dispatch-k6-breakpoint.yml)

#### GitHub Action with act
Running with act was mainly used for debugging github workflows without commiting and running from the github site. 
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
act workflow_dispatch \
-j k6-performance \
-s GITHUB_TOKEN=`gh auth token` \
--container-architecture linux/amd64 \
--artifact-server-path $HOME/.act \ 
--input vus=<vus> \
--input duration=<duration> \ 
--input testSuitePath=<path-to-testscript> 
```

Example of command:
```shell
act workflow_dispatch \
-j k6-performance \
-s GITHUB_TOKEN=`gh auth token` \
--container-architecture linux/amd64 \
--artifact-server-path $HOME/.act \ 
--input vus=10 \
--input duration=5m \ 
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
The test results from tests run from Github actions can be found in the GitHub Actions run log and [grafana](https://altinn-grafana-test-b2b8dpdkcvfuhfd3.eno.grafana.azure.com/d/ccbb2351-2ae2-462f-ae0e-f2c893ad1028/k6-prometheus). 
Otherwise, see the output from your CLI

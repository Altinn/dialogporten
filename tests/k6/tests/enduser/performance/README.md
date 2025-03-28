# Enduser get dialogs

This directory holds a performance test for all GET endpoints for `api/v1/enduser/dialogs`. The performance test is implemented in the `enduser-search.js` file. The purpose of this test is to measure the response time and performance of each endpoint when accessed sequentially for different end users. By running this test, you can identify any bottlenecks or performance issues in the API. This test can be executed from the command line or as part of a GitHub Actions workflow. For more information on how to run the test and view the results, refer to the sections below.

## Prerequisites
- [K6 prerequisites](../../README.md#Prerequisites)

## Test file
The test file associated with this performance test is 
- `enduser-search.js`

## Test description
The test has a list of enduser (ssn), and the following endpoints are visited in
sequence for each enduser:
- api/v1/enduser/dialogs?Party=urn:altinn:person:identifier-no:`<ssn>`
- api/v1/enduser/dialogs/`<dialogId>`
- api/v1/enduser/dialogs/`<dialogId>`/activities
- api/v1/enduser/dialogs/`<dialogId>`/activities/`<activityId>`
- api/v1/enduser/dialogs/`<dialogId>`/seenlog
- api/v1/enduser/dialogs/`<dialogId>`/seenlog/`<seenlogId>`
- api/v1/enduser/dialogs/`<dialogId>`/transmissions
- api/v1/enduser/dialogs/`<dialogId>`/transmissions/`<transmissionId>`
- api/v1/enduser/dialogs/`<dialogId>`/labellog

## Run test
### From cli
1. Navigate to the following directory:
```shell
cd tests/k6/tests/enduser/performance
```
2. Run the test using the following command. Replace `<test|staging|yt01>`, `<vus>`, and `<duration>` with the desired values:
```shell
TOKEN_GENERATOR_USERNAME=<username> TOKEN_GENERATOR_PASSWORD=<passwd> \
k6 run enduser-search.js -e API_VERSION=v1 \
-e API_ENVIRONMENT=<test|staging|yt01> \
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
--input testSuitePath=tests/k6/tests/enduser/performance/enduser-search.js
```

## Test Results
Test results can be found in GitHub action run log, grafana and in App Insights. 

# End-to-end and non-functional testing

## Introduction

This is the K6-based test project used to perform end-to-end testing as well as various kinds of performance testing of the Dialogporten APIs.

The various testsuites defined within are used in GitHub workflows, but can also be used standalone.

## Prerequisites

* Either
  * [Grafana K6](https://k6.io/) must be installed and `k6` available in `PATH` 
  * or Docker (available as `docker` in `PATH`)
* Powershell or Bash (should work on any platform supported by K6)

The test project is self-contained and has no dependency to any other Dialogporten-component.

## How to use

There is a central script for running test suites or individual tests, available as both Powershell and Bash. This script handles credentials, selecting environment to run the tests in and what API version to use (default `v1`).

Run `Get-Help .\run.ps1` or `./run.sh --help` for usage information.

The scripts will use locally installed `k6` if available. Failing that, it will attempt to use Docker with [grafana/k6](https://hub.docker.com/r/grafana/k6)

## Performance tests

For information about the performance tests, see the following README files:

- [ServiceOwner](./tests/serviceowner/performance/README.md)
- [GraphQL](./tests/graphql/performance/README.md)
- [EndUser](./tests/enduser/performance/README.md)

## Test suites

Various test suites are defined within the `suites` directory. A suite consists of 
* Importing the tests, or collection of tests to run
* Define [K6 options](https://k6.io/docs/using-k6/k6-options/) to configure test-run behavior
* Run the tests

See `suites/all-single-pass.js` for a basic example, running all tests with default options.

## How to create a test

Tests reside within `tests/serviceowner` and `tests/enduser` depending on the type of test.

Each file can contain several tests related to a particular functionality in the APIs. The test should export one default function containing the tests. 
> Make sure you do not introduce any global state; only create variables within the function scope.  All test files should be self-contained and handle both being run alone or with others in any sequential order, ie not rely on state created by tests in other files.

There are several utility functions provided in order to create tests, that can be imported from `testimports.js` residing the `common` directory.

Test files **must** clean up any state they create (see `purgeSO` below), to ensure that the test environment is in a consistent state. A check is implemented in `tests/all-tests.js` to ensure that all dialogs created during the gets purged. Failing to clean up will fail the test run. 

### Making requests

There are functions to perform a bearer token-authorized request to the service owner and end user endpoints for the selected environment and API version. The `path` parameter is appended to the base URL, can contain query parameters and should not contain a leading slash. `data` can be any javascript object, which will be serialized to JSON.

All these functions wrap the corresponding [k6/http](https://k6.io/docs/javascript-api/k6-http/) function.

The optional `params` argument can be supplied to set/override headers and other configuration. See https://k6.io/docs/javascript-api/k6-http/params/ for more information.

* `getSO(path, params = null)` 
* `postSO(path, data, params = null)`
* `putSO(path, data, params = null)`
* `patchSO(path, data, params = null)`, 
* `deleteSO(path, params = null)`, 
* `getEU(path, params = null)`
* `postEU(path, data, params = null)`
* `putEU(path, data, params = null)`
* `patchEU(path, data, params = null)`
* `deleteEU(path, data, params = null)`

### Checking responses

This project utilizes the [k6chaijs](https://k6.io/docs/javascript-api/jslib/k6chaijs/) library to support BDD assertions based on [ChaiJS](https://www.chaijs.com/)

* `describe(testName, testFunction)`: Defines a test and delegate that is invoked to perform the actual test. Wraps `group` in K6, and handles exceptions thrown by `expect`, logging errors to the console for easier debugging.
* `expect(data, expectationName)`: Chainable BDD-style function that allows various checks to be performed on `data`, where `expectationName` is any string describing the expectation

### Testdata

For performing write tests, various DTOs will have to be supplied. These should be placed within the `testdata` subdirectory. See `tests/serviceowner/testdata/01-create-dialog.js` for an example. Typically a "baseline" version of the DTO is defined, and various modifications to it can be perfomed within each test, instead of pre-defining every permutation as separate files.

### Example

A simple test file might look like this:

```js
import { describe, expect, getSO } from '../../common/testimports.js'

export default function () {
    describe('Perform simple dialog search', () => {
        let r = getSO('dialogs');
        expect(r.status, 'response status').to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf.at.least(1);
    });    
}
```

### Notes
- The request scripts uses the token generator from [Altinn Test Tools](https://github.com/Altinn/AltinnTestTools). The tokens produced contain all scopes required for all endpoints of Dialogporten, and is generated once per run, then re-used and refreshed as needed.

## TODO
* Add support for getting real Maskinporten tokens, see https://github.com/mulesoft-labs/js-client-oauth2

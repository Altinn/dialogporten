import { default as serviceOwnerTests } from './serviceowner/all-tests.js';
import { default as enduserTests } from './enduser/all-tests.js';
import { default as sentinelCheck } from '../common/sentinel.js';

export function runAllTests() {
    serviceOwnerTests();
    enduserTests();

    // Run sentinel check last, which will warn about and purge any leftover dialogs
    sentinelCheck();
};

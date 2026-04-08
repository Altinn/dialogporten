import { default as serviceOwnerTests } from './serviceowner/all-tests.js';
import { default as sentinelCheck } from '../common/sentinel.js';

export function runAllTests() {
    serviceOwnerTests();

    // Run sentinel check last, which will warn about and purge any leftover dialogs
    sentinelCheck();
};

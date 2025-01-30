/**
 * Performance test for creating dialogs with transmissions
 * Run: k6 run tests/k6/tests/serviceowner/performance/create-transmissions.js --vus 1 --iterations 1 -e numberOfTransmissions=100
 */
import { getDefaultThresholds } from '../../performancetest_common/getDefaultThresholds.js';
import { createTransmissions } from '../../performancetest_common/createDialog.js';
import { serviceOwners } from '../../performancetest_common/readTestdata.js';
import { validateTestData } from '../../performancetest_common/readTestdata.js';
export { setup as setup } from '../../performancetest_common/readTestdata.js';

export const options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: getDefaultThresholds(['http_req_duration', 'http_reqs'],['create dialog', 'create transmission'])
};

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const numberOfTransmissions = (__ENV.numberOfTransmissions ?? '10');
const maxTransmissionsInThread = (__ENV.maxTransmissionsInThread ?? '100');
const testid = (__ENV.TESTID ?? 'createTransmissions');
  
export default function(data) {
    const { endUsers, index } = validateTestData(data, serviceOwners);
    createTransmissions(serviceOwners[0], endUsers[index], traceCalls, numberOfTransmissions, maxTransmissionsInThread, testid);
}


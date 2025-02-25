import { randomItem } from '../../../common/k6-utils.js';
import { createTransmissions } from '../../performancetest_common/createDialog.js';
import { serviceOwners, endUsers } from '../../performancetest_common/readTestdata.js';

export const options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    vus: 1,
    duration: "30s",
    thresholds: {
        "http_req_duration{name:create dialog}": ["p(95)<300"],
        "http_reqs{name:create dialog}": [],
        "http_req_duration{name:create transmission}": ["p(95)<500"],
        "http_reqs{name:create transmission}": []
    }
};

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const numberOfTransmissions = (__ENV.numberOfTransmissions ?? '10');
const maxTransmissionsInThread = (__ENV.maxTransmissionsInThread ?? '100');
const testid = (__ENV.TESTID ?? 'createTransmissions');

export default function() {
    createTransmissions(serviceOwners[0], randomItem(endUsers), traceCalls, numberOfTransmissions, maxTransmissionsInThread, testid);
}

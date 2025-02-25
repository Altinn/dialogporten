import { createTransmissions } from '../../performancetest_common/createDialog.js';
import { serviceOwners, endUsersPart, validateTestData } from '../../performancetest_common/readTestdata.js';
import { randomItem } from '../../../common/k6-utils.js';

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const numberOfTransmissions = (__ENV.numberOfTransmissions ?? '10');
const maxTransmissionsInThread = (__ENV.maxTransmissionsInThread ?? '100');
const testid = (__ENV.TESTID ?? 'createTransmissions');
const stages_duration = (__ENV.stages_duration ?? '1m');
const stages_target = (__ENV.stages_target ?? '5');
const abort_on_fail = (__ENV.abort_on_fail ?? 'true') === 'true';

export const options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
        "http_req_duration{name:create dialog}": [{ threshold: "max<10000", abortOnFail: abort_on_fail }],
        "http_req_duration{name:create transmission}": [{ threshold: "max<10000", abortOnFail: abort_on_fail }],
        "http_reqs{name:create dialog}": [],
        "http_reqs{name:create transmission}": []
    },
    executor: 'per-vu-iterations',
    stages: [
        { duration: stages_duration, target: stages_target },
    ]
};

export function setup() {
    const totalVus = stages_target;
    let parts = [];
    for (let i = 1; i <= totalVus; i++) {
        parts.push(endUsersPart(totalVus, i));
    }
    return parts;
}

export default function(data) {
    const { endUsers, index } = validateTestData(data);
    createTransmissions(randomItem(serviceOwners), endUsers[index], traceCalls, numberOfTransmissions, maxTransmissionsInThread, testid);
}

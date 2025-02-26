import { randomItem } from '../../../common/k6-utils.js';
import { createDialog } from '../../performancetest_common/createDialog.js';
import { serviceOwners, endUsers } from '../../performancetest_common/readTestdata.js';
export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    vus: 1,
    duration: "30s",
    thresholds: {
        "http_req_duration{name:create dialog}": ["p(95)<300"],
        "http_reqs{name:create dialog}": []
    }
}

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

export default function () {
    createDialog(serviceOwners[0], randomItem(endUsers), traceCalls);
}

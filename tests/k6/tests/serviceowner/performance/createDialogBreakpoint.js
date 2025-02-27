import { createDialog } from '../../performancetest_common/createDialog.js'
import { endUsers, serviceOwners } from '../../performancetest_common/readTestdata.js';
import { randomItem } from '../../../common/k6-utils.js';

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const stages_duration = (__ENV.stages_duration ?? '1m');
const stages_target = (__ENV.stages_target ?? '5');
const abort_on_fail = (__ENV.abort_on_fail ?? 'true') === 'true';


export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
        "http_req_duration": [{ threshold: "max<10000", abortOnFail: abort_on_fail }],
    },
    executor: 'ramping-arrival-rate', //Assure load increase if the system slows
    stages: [
        { duration: stages_duration, target: stages_target }, // simulate ramp-up of traffic from 1 to stages_target users over stages_duration
    ],
}

export default function() {
    createDialog(randomItem(serviceOwners), randomItem(endUsers), traceCalls);
}

import { default as run, options as _options } from './serviceowner-search.js'
import { endUsersPart } from '../../performancetest_common/readTestdata.js';

const stages_duration = (__ENV.stages_duration ?? '1m');
const stages_target = (__ENV.stages_target ?? '5');
const abort_on_fail = (__ENV.abort_on_fail ?? 'true') === 'true';

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
        ..._options.thresholds,
        "http_req_duration{scenario:default}": [{ threshold: "max<5000", abortOnFail: abort_on_fail }],
    },
    executor: 'ramping-arrival-rate', //Assure load increase if the system slows
    stages: [
        { duration: stages_duration, target: stages_target }, // simulate ramp-up of traffic from 1 to stages_target users over stages_duration
    ],
}

export function setup() {
    const totalVus = stages_target;
    let parts = [];
    for (let i = 1; i <= totalVus; i++) {
        parts.push(endUsersPart(totalVus, i));
    }
    return parts;
}

export default function(data) { run(data);}


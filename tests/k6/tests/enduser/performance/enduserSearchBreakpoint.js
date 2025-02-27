import { default as run, setup as _setup, options as _options } from "./enduser-search.js";

const stages_duration = (__ENV.stages_duration ?? '1m');
const stages_target = (__ENV.stages_target ?? '5');
const abort_on_fail = (__ENV.abort_on_fail ?? 'true') === 'true';


export let options = {
    setupTimeout: '10m',
    summaryTrendStats: _options.summaryTrendStats,
    thresholds: {
        ..._options.thresholds,
        "http_req_duration{scenario:default}": [{ threshold: "max<5000", abortOnFail: abort_on_fail }]
    },
    executor: 'ramping-arrival-rate', //Assure load increase if the system slows
    stages: [
        { duration: stages_duration, target: stages_target }, // simulate ramp-up of traffic from 1 to stages_target users over stages_duration
    ],
}

export function setup() { return _setup(); }
export default function(data) { run(data);}
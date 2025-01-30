import { default as run } from "./create-dialog.js";
export { setup as setup } from '../../performancetest_common/readTestdata.js';

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    vus: 1,
    duration: "30s",
    thresholds: {
        "http_req_duration": ["p(95)<300", "p(99)<500"],    
    }
}

export default function (data) { run(data); }
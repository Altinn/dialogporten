import { default as run } from "./serviceowner-search.js";
export { setup as setup } from '../../performancetest_common/readTestdata.js';

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    vus: 1,
    duration: "30s",
    thresholds: {
        "http_req_duration{name:serviceowner search}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get dialog activities}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get dialog activity}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get seenlogs}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get transmissions}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get transmission}": ["p(95)<100", "p(99)<300"],
        "http_reqs{name:get dialog activities}": [],
        "http_reqs{name:get dialog activity}": [],
        "http_reqs{name:get seenlogs}": [],
        "http_reqs{name:get transmissions}": [],
        "http_reqs{name:get transmission}": [],
        "http_reqs{name:serviceowner search}": [],   
    }
}

export default function (data) { run(data); }


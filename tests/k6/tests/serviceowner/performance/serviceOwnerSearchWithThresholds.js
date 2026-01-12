import { default as run, options as _options } from "./serviceowner-search.js";
export { setup } from "./serviceowner-search.js";

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    vus: 1,
    duration: "30s",
    thresholds: {
        ..._options.thresholds,
        "http_req_duration{name:serviceowner search}": ["p(95)<500"],
        "http_req_duration{name:get dialog}": ["p(95)<500"],
        "http_req_duration{name:get dialog activities}": ["p(95)<300"],
        "http_req_duration{name:get dialog activity}": ["p(95)<300"],
        "http_req_duration{name:get seenlogs}": ["p(95)<300"],
        "http_req_duration{name:get seenlog}": ["p(95)<300"],
        "http_req_duration{name:get transmissions}": ["p(95)<300"],
        "http_req_duration{name:get transmission}": ["p(95)<300"],
    }
}

export default function (data) { run(data); }


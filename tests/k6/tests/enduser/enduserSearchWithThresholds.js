import { default as run } from "./performance/enduser-search.js";

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    vus: 1,
    duration: "30s",
    thresholds: {
        "http_req_duration{name:get dialog}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get dialog activities}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get dialog activity}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get seenlogs}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get transmissions}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get transmission}": ["p(95)<100", "p(99)<300"],
        "http_req_duration{name:get labellog}": ["p(95)<100", "p(99)<300"],
        "http_reqs{name:get dialog activities}": [],
        "http_reqs{name:get dialog activity}": [],
        "http_reqs{name:get seenlogs}": [],
        "http_reqs{name:get transmissions}": [],
        "http_reqs{name:get transmission}": [],
        "http_reqs{name:get dialog}": [], 
        "http_reqs{name:get labellog}": [], 
    }
}

export default function (data) { run(data); }


import { default as run, setup as _setup } from "./graphql-search.js";
import { labels } from "./graphql-search.js";

const numberOfEndUsers = 200; // Remove when altinn-testtools bulk get of endusers/tokens is fast
export const options = {
    setupTimeout: '10m',
    vus: 1,
    duration: "30s",
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {}
};

for (var label of labels) {
    options.thresholds[`http_req_duration{name:${label}}`] = ["p(95)<500"];
    options.thresholds[`http_reqs{name:${label}}`] = [];
}

export function setup() { return _setup(numberOfEndUsers); }
export default function (data) { run(data); }


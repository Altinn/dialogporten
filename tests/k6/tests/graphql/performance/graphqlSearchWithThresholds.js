import { _default, _setup } from "./graphqlCommonFunctions.js";

const label = "graphql-getall-dialogs-for-party";
const queryType = "getAllDialogsForParty"

const numberOfEndUsers = 200; // Remove when altinn-testtools bulk get of endusers/tokens is fast
export const options = {
    setupTimeout: '10m',
    vus: 1,
    duration: "30s",
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {}
};


options.thresholds[`http_req_duration{name:${label}}`] = ["p(95)<500"];
options.thresholds[`http_reqs{name:${label}}`] = [];


export function setup() { return _setup(label, queryType, numberOfEndUsers); }
export default function (data) { _default(data); }
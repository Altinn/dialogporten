import http from 'k6/http';
import { randomItem, uuidv4, URL} from '../../../common/k6-utils.js';
import { getEndUserTokens } from '../../../common/token.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { baseUrlEndUser } from '../../../common/config.js';
const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const defaultNumberOfEndUsers = (__ENV.NUMBER_OF_ENDUSERS ?? 2799); // Max number of endusers from altinn-testtools now.

const texts = [ "påkrevd", "rapportering", "sammendrag", "Utvidet Status", "ingen HTML-støtte", "et eller annet", "Skjema", "Skjema for rapportering av et eller annet", "Maks 200 tegn", "liste" ];
const texts_no_hit = [ "sjøvegan", "larvik", "kvalsund", "jøssheim", "sørli"];
const resources = [ 
    "ttd-dialogporten-performance-test-01", 
    "ttd-dialogporten-performance-test-02", 
    "ttd-dialogporten-performance-test-03", 
    "ttd-dialogporten-performance-test-04", 
    "ttd-dialogporten-performance-test-05", 
    "ttd-dialogporten-performance-test-06", 
    "ttd-dialogporten-performance-test-07", 
    "ttd-dialogporten-performance-test-08", 
    "ttd-dialogporten-performance-test-09", 
    "ttd-dialogporten-performance-test-10"
];

// Available enduser filters:
// org, party, serviceResource, extendedStatus, externalReference, status, createdAfter, createdBefore, updatedAfter, updatedBefore, dueAfter, dueBefore, Search, searchLanguageCode, orderBy, limit

const filter_combos = [
    {label: "serviceresource", filters: ["serviceResource"]},
    {label: "serviceresource-createdafter", filters: ["serviceResource", "createdAfter"]},
    {label: "serviceresource-createdbefore", filters: ["serviceResource", "createdBefore"]},
    {label: "party-createdafter", filters: ["party", "createdAfter"]},
    {label: "party-createdbefore", filters: ["party", "createdBefore"]},
    {label: "search-party-createdafter", filters: ["Search", "party", "createdAfter"]},
    {label: "search-serviceresource-createdafter", filters: ["Search", "serviceResource", "createdAfter"]},
    {label: "search-serviceresource-createdafter-nohit", filters: ["Search", "serviceResource", "createdAfter"]},
    {label: "search-serviceresource", filters: ["Search", "serviceresource"]},
    {label: "search-party-createdafter", filters: ["Search", "party", "createdAfter"]},
    {label: "search-party-createdafter-nohit", filters: ["Search", "party", "createdAfter"]},
    {label: "search-party", filters: ["Search", "party"]},

];

export let options = {
    setupTimeout: '10m',
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
        checks: ['rate>=1.0']
    }
};

for (var filter of filter_combos) {
    options.thresholds[[`http_req_duration{name:${filter.label}}`]] = [];
    options.thresholds[[`http_req_failed{name:${filter.label}}`]] = ['rate<=0.0'];
}

function get_query_params(endUser) {
    var search_params = {};
    var filter_combo = randomItem(filter_combos);
    var label = filter_combo.label
    for (var filter of filter_combo.filters) {
        search_params[filter] = get_filter_value(filter, label, endUser)
    }
    return [search_params, label];
}

function get_filter_value(filter, label, endUser) {
    switch (filter) {
        case "serviceResource": return "urn:altinn:resource:" +randomItem(resources);
        case "party": return "urn:altinn:person:identifier-no:" +endUser;
        case "status": return "New";
        case "deleted": return "Exclude";
        case "createdAfter": return new Date(Date.now() - 7*24*60*60*1000).toISOString();
        case "createdBefore": return new Date(Date.now() - 7*24*60*60*1000).toISOString();
        case "Search": return label.includes("nohit") ? randomItem(texts_no_hit) : randomItem(texts);
        default: return "urn:altinn:resource:" +randomItem(resources);
    }
}

export function setup(numberOfEndUsers = defaultNumberOfEndUsers) {
    const tokenOptions = {
        scopes: "digdir:dialogporten"
    }
    if (numberOfEndUsers === null) {
        numberOfEndUsers = defaultNumberOfEndUsers;
    }
    const endusers = getEndUserTokens(numberOfEndUsers, tokenOptions);
    return endusers
}

export default function(data) {
    const endUser = randomItem(Object.keys(data));
    const [queryParams, label] = get_query_params(endUser);
    const token = data[endUser];
    const traceparent = uuidv4();
    const paramsWithToken = {
        headers: {
            Authorization: "Bearer " + token,
            traceparent: traceparent,
            Accept: 'application/json',
            'User-Agent': 'dialogporten-k6',
        },
        tags: { name: label }
    }

    if (traceCalls) {
        paramsWithToken.tags.traceparent = traceparent;
    }

    const url = new URL(baseUrlEndUser + 'dialogs');
    for (const key in queryParams) {
        url.searchParams.append(key, queryParams[key]);    
    }

    describe('Perform enduser dialog list', () => {
        let r = http.get(url.toString(), paramsWithToken);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        return r
    });
}




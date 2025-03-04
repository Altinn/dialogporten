import http from 'k6/http';
import { serviceOwners, endUsers } from '../../performancetest_common/readTestdata.js';
import { randomItem, uuidv4, URL} from '../../../common/k6-utils.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { baseUrlServiceOwner } from '../../../common/config.js';
import { getEnterpriseToken } from '../../performancetest_common/getTokens.js';
const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

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
// party, endUser, serviceResource, status, deleted, createdAfter, createdBefore, updatedAfter, updatedBefore, dueAfter, dueBefore, visibleAfter, visibleBefore, Search, searchLanguageCode, orderBy, limit

const filter_combos = [
    {label: "enduser", filters: ["endUser"]},
    {label: "enduser-serviceresource", filters: ["endUser", "serviceResource"]},
    {label: "enduser-serviceresource-createdafter", filters: ["endUser", "serviceResource", "createdAfter"]},
    {label: "enduser-serviceresource-createdbefore", filters: ["endUser", "serviceResource", "createdBefore"]},
    {label: "enduser-createdafter", filters: ["endUser", "createdAfter"]},
    {label: "enduser-createdbefore", filters: ["endUser", "createdBefore"]},
    {label: "search-enduser-createdafter", filters: ["Search", "endUser", "createdAfter"]},
    {label: "search-createdafter", filters: ["Search", "createdAfter"]},
    {label: "search-serviceresource-enduser-createdafter", filters: ["Search", "serviceResource", "endUser", "createdAfter"]},
    {label: "search-enduser-createdafter-nohit", filters: ["Search", "endUser", "createdAfter"]},
    {label: "search", filters: ["Search"]},

];

const orgNos = ["713431400"]


export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
        checks: ['rate>=1.0']
    }
};

for (var filter of filter_combos) {
    options.thresholds[[`http_req_duration{name:${filter.label}}`]] = [];
    options.thresholds[[`http_req_failed{name:${filter.label}}`]] = ['rate<=0.0'];
}

function get_query_params() {
    var search_params = {};
    var filter_combo = randomItem(filter_combos);
    var label = filter_combo.label
    for (var filter of filter_combo.filters) {
        search_params[filter] = get_filter_value(filter, label)
    }
    return [search_params, label];
}

function get_filter_value(filter, label) {
    switch (filter) {
        case "endUser": return "urn:altinn:person:identifier-no:" +randomItem(endUsers).ssn;
        case "serviceResource": return "urn:altinn:resource:" +randomItem(resources);
        case "party": return "urn:altinn:organization:identifier-no:" +randomItem(orgNos);
        case "status": return "New";
        case "deleted": return "Exclude";
        case "createdAfter": return new Date(Date.now() - 7*24*60*60*1000).toISOString();
        case "createdBefore": return new Date(Date.now() - 7*24*60*60*1000).toISOString();
        case "Search": return label.includes("nohit") ? randomItem(texts_no_hit) : randomItem(texts);
        default: return "urn:altinn:resource:" +randomItem(resources);
    }
}

export default function() {
    const [queryParams, label] = get_query_params();
    const serviceowner = serviceOwners[0];
    const traceparent = uuidv4();
    const paramsWithToken = {
        headers: {
            Authorization: "Bearer " + getEnterpriseToken(serviceowner),
            traceparent: traceparent,
            Accept: 'application/json',
            'User-Agent': 'dialogporten-k6',
        },
        tags: { name: label }
    }

    if (traceCalls) {
        paramsWithToken.tags.traceparent = traceparent;
    }

    const url = new URL(baseUrlServiceOwner + 'dialogs');
    for (const key in queryParams) {
        url.searchParams.append(key, queryParams[key]);    
    }

    describe('Perform serviceowner dialog list', () => {
        let r = http.get(url.toString(), paramsWithToken);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        return r
    });
}




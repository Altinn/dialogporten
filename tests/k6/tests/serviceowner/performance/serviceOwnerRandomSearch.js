import http from 'k6/http';
import { serviceOwners, endUsers } from '../../performancetest_common/readTestdata.js';
import { randomItem, uuidv4, URL} from '../../../common/k6-utils.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { baseUrlServiceOwner } from '../../../common/config.js';
import { getEnterpriseToken } from '../../performancetest_common/getTokens.js';
import { texts, texts_no_hit, resources } from '../../performancetest_common/readTestdata.js';
const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

// Available serviceowner filters:
// party, endUser, serviceResource, status, deleted, createdAfter, createdBefore, updatedAfter, updatedBefore, dueAfter, dueBefore, visibleAfter, visibleBefore, Search, searchLanguageCode, orderBy, limit

const filter_combos = [
    {label: "enduser-party", filters: ["endUserId", "party"]},
    {label: "enduser-serviceresource", filters: ["endUserId", "serviceResource"]},
    {label: "enduser-serviceresource-createdafter", filters: ["endUserId", "serviceResource", "createdAfter"]},
    {label: "enduser-serviceresource-createdbefore", filters: ["endUserId", "serviceResource", "createdBefore"]},
    {label: "enduser-createdafter-party", filters: ["endUserId", "createdAfter", "party"]},
    {label: "enduser-createdbefore-party", filters: ["endUserId", "createdBefore", "party"]},
    {label: "search-enduser-createdafter-party", filters: ["Search", "endUserId", "createdAfter", "party"]},
    {label: "search-serviceresource-enduserid-createdafter", filters: ["Search", "serviceResource", "endUserId", "createdAfter"]},
    {label: "search-enduser-createdafter-party-nohit", filters: ["Search", "endUserId", "createdAfter", "party"]},
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
        case "endUserId": return "urn:altinn:person:identifier-no:" +randomItem(endUsers).ssn;
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




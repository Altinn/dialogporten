import { randomItem, uuidv4 } from '../../../common/k6-utils.js';
import { getEndUserTokens } from '../../../common/token.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { getGraphqlRequestBodyForAllDialogsForParty } from '../../performancetest_data/graphql-queries.js';
import { postGQ } from '../../../common/request.js';
import { texts, texts_no_hit, resources } from '../../performancetest_common/readTestdata.js';
import { log } from '../../performancetest_common/simpleSearch.js';

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const defaultNumberOfEndUsers = (__ENV.NUMBER_OF_ENDUSERS ?? 2799); // Max number of endusers from altinn-testtools now.

const filter_combos = [
    {label: "serviceresource", filters: ["serviceResource"]},
    {label: "createdafter", filters: ["createdAfter"]},
    {label: "serviceresource-createdafter", filters: ["serviceResource", "createdAfter"]},
    {label: "serviceresource-createdbefore", filters: ["serviceResource", "createdBefore"]},
    {label: "party-createdafter", filters: ["partyURIs", "createdAfter"]},
    {label: "party-createdbefore", filters: ["partyURIs", "createdBefore"]},
    {label: "search-serviceresource-createdafter", filters: ["search", "serviceResource", "createdAfter"]},
    {label: "search-serviceresource-createdbefore", filters: ["search", "serviceResource", "createdBefore"]},
    {label: "search-serviceresource-createdafter-nohit", filters: ["search", "serviceResource", "createdAfter"]},
    {label: "search-serviceresource", filters: ["search", "serviceResource"]},
    {label: "search-party-createdafter", filters: ["search", "partyURIs", "createdAfter"]},
    {label: "search-party-createdbefore", filters: ["search", "partyURIs", "createdBefore"]},
    {label: "search-party-createdafter-nohit", filters: ["search", "partyURIs", "createdAfter"]},
    {label: "search-party-createdbefore-nohit", filters: ["search", "partyURIs", "createdBefore"]},
    {label: "search-party", filters: ["search", "partyURIs"]},
    {label: "search-party-nohit", filters: ["search", "partyURIs"]},
    {label: "party", filters: ["party"]}
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
        case "serviceResource": return ["urn:altinn:resource:" +randomItem(resources)];
        case "party": return ["urn:altinn:person:identifier-no:" +endUser];
        case "status": return ["NOT_APPLICABLE", "IN_PROGRESS", "AWAITING", "REQUIRES_ATTENTION", "COMPLETED"] ;
        case "createdAfter": return new Date(Date.now() - 7*24*60*60*1000).toISOString();
        case "createdBefore": return new Date(Date.now() - 7*24*60*60*1000).toISOString();
        case "search": return label.includes("nohit") ? randomItem(texts_no_hit) : randomItem(texts);
        default: return ["urn:altinn:resource:" +randomItem(resources)];
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
    const [searchParams, label] = get_query_params(endUser);
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

    describe('Perform graphql dialog list', () => {
        let r = postGQ(getGraphqlRequestBodyForAllDialogsForParty(searchParams), paramsWithToken);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        log(r.json().data?.searchDialogs?.items, traceCalls, endUser, r.timings.duration);
    });
}



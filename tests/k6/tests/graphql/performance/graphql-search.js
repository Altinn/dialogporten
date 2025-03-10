/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphql-search.js --vus 1 --iterations 1 -e env=yt01
 */
import { randomItem } from '../../../common/k6-utils.js';
import { graphqlSearch } from "../../performancetest_common/simpleSearch.js";
import { getEndUserTokens } from '../../../common/token.js';

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const numberOfEndUsers = (__ENV.NUMBER_OF_ENDUSERS ?? 2799); // Max number of endusers from altinn-testtools now.


/**
 * The options object for configuring the performance test for GraphQL search.
 *
 * @property {string[]} summaryTrendStats - The summary trend statistics to include in the test results.
 * @property {object} thresholds - The thresholds for the test metrics.
 */
export const options = {
    setupTimeout: '10m',
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
        "http_req_duration{scenario:default}": [],
        "http_req_duration{name:graphql search}": [],
        "http_reqs{scenario:default}": [],
        "http_reqs{name:graphql search}": [],
    }
};

export function setup() {
    const tokenOptions = {
        scopes: "digdir:dialogporten"
    }
    const endusers = getEndUserTokens(numberOfEndUsers, tokenOptions);
    return endusers
}

export default function(data) {
    const endUser = randomItem(Object.keys(data));
    const token = data[endUser];
    const searchParams = { party: `urn:altinn:person:identifier-no:${endUser}`}
    graphqlSearch(endUser, searchParams, token, traceCalls);
}

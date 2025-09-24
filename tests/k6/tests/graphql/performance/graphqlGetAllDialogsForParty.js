/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphql-search.js --vus 1 --iterations 1 -e env=yt01
 */
import { randomItem } from '../../../common/k6-utils.js';
import { getEndUserTokens } from '../../../common/token.js';
import { getGraphqlRequestBodyForAllDialogsForParty } from '../../performancetest_data/graphql-queries.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { postGQ } from '../../../common/request.js';
import { log } from '../../performancetest_common/simpleSearch.js';

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const defaultNumberOfEndUsers = (__ENV.NUMBER_OF_ENDUSERS ?? 2799);; // Max number of endusers from altinn-testtools now.
const graphql_enduser_search = "graphql-enduser-search";
export const labels = [graphql_enduser_search];

/**
 * The options object for configuring the performance test for GraphQL search.
 *
 * @property {string[]} summaryTrendStats - The summary trend statistics to include in the test results.
 * @property {object} thresholds - The thresholds for the test metrics.
 */
export const options = {
  setupTimeout: '10m',
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
  thresholds: {}
};

for (var label of labels) {
  options.thresholds[`http_req_duration{name:${label}}`] = [];
  options.thresholds[`http_reqs{name:${label}}`] = [];
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

export default function (data) {
  const endUser = randomItem(Object.keys(data));
  var label = graphql_enduser_search
  const token = data[endUser];
  const paramsWithToken = {
    headers: {
      Authorization: "Bearer " + token,
      Accept: 'application/json',
      'User-Agent': 'dialogporten-k6',
    },
    tags: { name: label }
  }
  const variables = {
    partyURIs: [`urn:altinn:person:identifier-no:${endUser}`],
    limit: 100,
    label: ["DEFAULT"],
    status: ["NOT_APPLICABLE", "IN_PROGRESS", "AWAITING", "REQUIRES_ATTENTION", "COMPLETED"]
  }
  describe('Perform graphql dialog list', () => {
    let r = postGQ(getGraphqlRequestBodyForAllDialogsForParty(variables), paramsWithToken);
    expectStatusFor(r).to.equal(200);
    expect(r, 'response').to.have.validJsonBody();
    log(r.json().data.searchDialogs.items, traceCalls, endUser, r.timings.duration);
  });
}

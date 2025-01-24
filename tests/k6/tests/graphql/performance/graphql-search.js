/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphql-search.js --vus 1 --iterations 1 -e env=yt01
 */
import exec from 'k6/execution';
import { getDefaultThresholds } from '../../performancetest_common/getDefaultThresholds.js';
import { endUsersPart } from '../../performancetest_common/readTestdata.js';
import { graphqlSearch } from "../../performancetest_common/simpleSearch.js";

const isSingleUserMode = (__ENV.isSingleUserMode ?? 'false') === 'true';    
const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';


/**
 * The options object for configuring the performance test for GraphQL search.
 *
 * @property {string[]} summaryTrendStats - The summary trend statistics to include in the test results.
 * @property {object} thresholds - The thresholds for the test metrics.
 */
export const options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: getDefaultThresholds(['http_req_duration', 'http_reqs'],['graphql search'])
};

export function setup() {
    const totalVus = exec.test.options.scenarios.default.vus;
    let parts = [];
    for (let i = 1; i <= totalVus; i++) {
        parts.push(endUsersPart(totalVus, i));
    }
    return parts;
  }

export default function(data) {
    const myEndUsers = data[exec.vu.idInTest - 1];
    const ix = exec.vu.iterationInInstance % myEndUsers.length;
    graphqlSearch(myEndUsers[ix], traceCalls);  
}



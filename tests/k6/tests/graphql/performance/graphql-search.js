/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphql-search.js --vus 1 --iterations 1 -e env=yt01
 */
import { getDefaultThresholds } from '../../performancetest_common/getDefaultThresholds.js';
import { validateTestData } from '../../performancetest_common/readTestdata.js';
import { graphqlSearch } from "../../performancetest_common/simpleSearch.js";
export { setup as setup } from '../../performancetest_common/readTestdata.js';

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

export default function(data) {
    const { endUsers, index } = validateTestData(data);
    graphqlSearch(endUsers[index], traceCalls);  
}



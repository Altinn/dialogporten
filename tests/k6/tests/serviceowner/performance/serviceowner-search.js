import { serviceownerSearch } from '../../performancetest_common/simpleSearch.js'
import { getDefaultThresholds } from '../../performancetest_common/getDefaultThresholds.js';
import { serviceOwners } from '../../performancetest_common/readTestdata.js';
import { validateTestData } from '../../performancetest_common/readTestdata.js';
export { setup as setup } from '../../performancetest_common/readTestdata.js';

const tag_name = 'serviceowner search';
const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: getDefaultThresholds(['http_req_duration', 'http_reqs'],[tag_name, 
        'get dialog', 
        'get dialog activities', 
        'get dialog activity', 
        'get seenlogs',
        'get seenlog',
        'get transmissions',
        'get transmission'])
};
  
export default function(data) {
    const { endUsers, index } = validateTestData(data, serviceOwners);
    serviceownerSearch(serviceOwners[0], endUsers[index], tag_name, traceCalls);
}


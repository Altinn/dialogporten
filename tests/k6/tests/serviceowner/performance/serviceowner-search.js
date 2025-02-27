import { serviceownerSearch, emptySearchThresholds } from '../../performancetest_common/simpleSearch.js'
import { serviceOwners, endUsers } from '../../performancetest_common/readTestdata.js';
import { randomItem } from '../../../common/k6-utils.js';
const tag_name = 'serviceowner search';
const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
        ...emptySearchThresholds,
        "http_req_duration{name:serviceowner search}": [],
        "http_reqs{name:serviceowner search}": []
    }
};


export default function(data) {
    serviceownerSearch(serviceOwners[0], randomItem(endUsers), tag_name, traceCalls);
}

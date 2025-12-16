import { serviceownerPartySearch, emptySearchThresholds } from '../../performancetest_common/simpleSearch.js'
import { serviceOwners, parties } from '../../performancetest_common/readTestdata.js';
import { randomItem } from '../../../common/k6-utils.js';
const tag_name = 'serviceowner party filter';
const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
        ...emptySearchThresholds,
        "http_req_duration{name:serviceowner party filter}": [],
        "http_reqs{name:serviceowner party filter}": []
    }
};


export default function(data) {
    serviceownerPartySearch(serviceOwners[0], randomItem(parties), tag_name, traceCalls);
}

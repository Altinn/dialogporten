import { enduserSearch, emptySearchThresholds } from '../../performancetest_common/simpleSearch.js'
import { getEndUserTokens } from '../../../common/token.js';
import { randomItem } from '../../../common/k6-utils.js';

const defaultNumberOfEndUsers = (__ENV.NUMBER_OF_ENDUSERS ?? 2799); // Max number of endusers from altinn-testtools now.

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

export let options = {
    setupTimeout: '10m',
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
       ...emptySearchThresholds,
       "http_req_duration{name:enduser search}": [],
       "http_reqs{name:enduser search}": [],
       "http_req_duration{name:get labellog}": [],
       "http_reqs{name:get labellog}": []
    }
};

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
    const token = data[endUser];
    enduserSearch(endUser, token, traceCalls);
}

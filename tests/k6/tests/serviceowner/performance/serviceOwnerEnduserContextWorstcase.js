import http from 'k6/http';
import { getEnterpriseToken } from '../../performancetest_common/getTokens.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { baseUrlServiceOwner } from '../../../common/config.js';
import { serviceOwners } from '../../performancetest_common/readTestdata.js';

const users = [
    { pid: "06917699338", label: "a_06917699338_73k" },
    { pid: "23826098759", label: "b_23826098759_62k" },
    { pid: "28866696375", label: "c_28866696375_52k" },
    { pid: "16896795523", label: "d_16896795523_41k" },
    { pid: "02875998768", label: "e_02875998768_30k" },
    { pid: "06925999758", label: "f_06925999758_20k" },
    { pid: "19866498574", label: "g_19866498574_10k" },
    { pid: "30906099140", label: "h_30906099140_2k" },
]

export let options = {
    vus: 1,
    iterations: users.length * 2, // Each user will perform 2 iterations, total 16 iterations.
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'count'],
    thresholds: {
    }
};

for (var filter of users) {
    options.thresholds[[`http_req_duration{name:${filter.label}}`]] = [];
    options.thresholds[[`http_req_failed{name:${filter.label}}`]] = ['rate<=0.0'];
}

export function setup() {
    const token = getEnterpriseToken(serviceOwners[0]);
    return token;
}


export default function (token) {
    const endUser = users[__ITER % users.length];
    const pid = endUser.pid;
    const label = endUser.label;
    const queryParams = {
        party: "urn:altinn:person:identifier-no:" + pid,
        limit: 1000
    }

    const paramsWithToken = {
        headers: {
            Authorization: "Bearer " + token, //getEnterpriseToken(serviceOwners[0]),
        },
        tags: { name: label }
    }

    const url = new URL(baseUrlServiceOwner + 'dialogs/endusercontext');
    for (const key in queryParams) {
        url.searchParams.append(key, queryParams[key]);
    }

    describe('Perform enduser dialog serviceResource', () => {
        let r = http.get(url.toString(), paramsWithToken);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        return r
    });
}

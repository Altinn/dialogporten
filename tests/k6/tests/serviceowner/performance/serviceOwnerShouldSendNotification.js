/**
 * Performance test for Service Owner API - shouldSendNotification
 * Run: k6 run tests/k6/tests/serviceowner/performance/serviceOwnerShouldSendNotification.js --vus 1 --iterations 1 -e env=yt01 
 * Currently hard-coded to test for NotExists and TransmissionOpened to match skatt and skattekort
 */

import http from 'k6/http';
import { dialogsWithTransmissions } from '../../performancetest_common/readTestdata.js';
import { randomItem, uuidv4, URL} from '../../../common/k6-utils.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { baseUrlServiceOwner } from '../../../common/config.js';
import { getEnterpriseToken } from '../../performancetest_common/getTokens.js';
import { getOptions } from '../../performancetest_common/thresholdConfig.js';
const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

const orgNos = ["713431400"];
const label = "should-send-notifications";

export let options = getOptions([label]);

export default function() {
    const traceparent = uuidv4();
    const tokenOpts = { scopes: 'altinn:system/notifications.condition.check', org: 'test', orgno: randomItem(orgNos) };
    const dialogWithTransmission = randomItem(dialogsWithTransmissions);
    const paramsWithToken = {
        headers: {
            Authorization: "Bearer " + getEnterpriseToken(tokenOpts),
            traceparent: traceparent,
            Accept: 'application/json',
            'User-Agent': 'dialogporten-k6',
        },
        tags: { name: label }
    }

    if (traceCalls) {
        paramsWithToken.tags.traceparent = traceparent;
    }
    const url = new URL(baseUrlServiceOwner + 'dialogs' + `/${dialogWithTransmission.dialogId}` + '/actions/should-send-notification');
    // Testing only for NotExists and TransmissionOpened for now, to match skatt and skattekort 
    url.searchParams.append('conditionType', 'NotExists');
    url.searchParams.append('activityType', 'TransmissionOpened'); 
    url.searchParams.append('transmissionId', dialogWithTransmission.transmissionId);

    describe('Perform should send notification', () => {
        let r = http.get(url.toString(), paramsWithToken);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
    });
}

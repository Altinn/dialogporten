/**
 * @fileoverview Performance test for creating dialog, transmission, and activity
 * A test to emulate the way skatteetaten creates skattekort-dialogs
 * * Run: k6 run tests/k6/tests/serviceowner/performance/createDialogTransmissionActivity.js --vus 1 --iterations 1
 */

import { CreateDialogTransmissionAndActivity } from "../../performancetest_common/createDialog.js";
import { serviceOwners, validateTestData } from "../../performancetest_common/readTestdata.js";
import { getOptions } from "../../performancetest_common/thresholdConfig.js";
export { setup as setup } from "../../performancetest_common/readTestdata.js";

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const testid = (__ENV.TESTID ?? 'createDialogTransmissionActivity');

const dialogLabel = "create dialog";
const transmissionLabel = "create transmission";
const activityLabel = "create activity";

const labels = [dialogLabel, transmissionLabel, activityLabel];

export let options = getOptions(labels);

export default function(data) {
  const { endUsers, index } = validateTestData(data, serviceOwners);
  CreateDialogTransmissionAndActivity(serviceOwners[0], endUsers[index], traceCalls, testid);
}

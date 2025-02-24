/**
 * Performance test for creating and removing a dialog
 * Run: k6 run tests/k6/tests/serviceowner/performance/create-remove-dialog.js --vus 1 --iterations 1
 */
import { serviceOwners } from "../../performancetest_common/readTestdata.js";
import { createAndRemoveDialog } from '../../performancetest_common/createDialog.js';
import { validateTestData } from '../../performancetest_common/readTestdata.js';
export { setup as setup } from '../../performancetest_common/readTestdata.js';

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
      "http_req_duration{name:create dialog}": [],
      "http_reqs{name:create dialog}": [],
      "http_req_duration{name:remove dialog}": [],
      "http_reqs{name:remove dialog}": []
  }
};

export default function(data) {
  const { endUsers, index } = validateTestData(data, serviceOwners);
  createAndRemoveDialog(serviceOwners[0], endUsers[index], traceCalls);
}

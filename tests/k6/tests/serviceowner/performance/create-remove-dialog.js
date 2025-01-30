/** 
 * Performance test for creating and removing a dialog
 * Run: k6 run tests/k6/tests/serviceowner/performance/create-remove-dialog.js --vus 1 --iterations 1
 */
import { getDefaultThresholds } from '../../performancetest_common/getDefaultThresholds.js';
import { serviceOwners } from "../../performancetest_common/readTestdata.js";
import { createAndRemoveDialog } from '../../performancetest_common/createDialog.js';
import { validateTestData } from '../../performancetest_common/readTestdata.js';
export { setup as setup } from '../../performancetest_common/readTestdata.js';

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: getDefaultThresholds(['http_req_duration', 'http_reqs'],[
      'create dialog',
      'remove dialog'
    ])
};

export default function(data) {
  const { endUsers, index } = validateTestData(data, serviceOwners);
  createAndRemoveDialog(serviceOwners[0], endUsers[index], traceCalls);
}
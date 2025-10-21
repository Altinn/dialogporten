import { CreateDialogTransmissionAndActivity } from "../../performancetest_common/createDialog.js";
import { serviceOwners, validateTestData } from "../../performancetest_common/readTestdata.js";
export { setup as setup } from "../../performancetest_common/readTestdata.js";

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const breakpoint = (__ENV.BREAKPOINT ?? 'false') === 'true';
const abort_on_fail = (__ENV.ABORT_ON_FAIL ?? 'false') === 'true';
const stages_duration = __ENV.stages_duration ?? '5m';
const stages_target = __ENV.stages_target ?? 10;
const testid = (__ENV.TESTID ?? 'createDialogTransmissionActivity');

const dialogLabel = "create dialog";
const transmissionLabel = "create transmission";
const activityLabel = "create activity";

const labels = [dialogLabel, transmissionLabel, activityLabel];

function getOptions(labels) {
  const options = {
    setupTimeout: '10m',
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'count'],
    thresholds: {}
  };

  if (breakpoint) {
    for (const label of labels) {
      options.thresholds[`http_req_duration{name:${label}}`] = [{ threshold: "max<5000", abortOnFail: abort_on_fail }];
      options.thresholds[`http_req_failed{name:${label}}`] = [{ threshold: 'rate<=0.0', abortOnFail: abort_on_fail }];
      options.thresholds[`http_reqs{name:${label}}`] = [];
    }
    
    options.stages = [
      { duration: stages_duration, target: stages_target },
    ];
  } else {
    for (const label of labels) {
      options.thresholds[`http_req_duration{name:${label}}`] = [];
      options.thresholds[`http_req_failed{name:${label}}`] = [];
      options.thresholds[`http_reqs{name:${label}}`] = [];
    }
  }
  return options;
}

export let options = getOptions(labels);

export default function(data) {
  const { endUsers, index } = validateTestData(data, serviceOwners);
  const [dialogId, transmissionId, activityId] = CreateDialogTransmissionAndActivity(serviceOwners[0], endUsers[index], traceCalls, testid);
}
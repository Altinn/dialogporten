const breakpoint = (__ENV.BREAKPOINT ?? 'false') === 'true';
const abort_on_fail = (__ENV.ABORT_ON_FAIL ?? 'false') === 'true';
const stages_duration = __ENV.stages_duration ?? '5m';
const stages_target = parseInt(__ENV.stages_target ?? '10', 10);

/**
 * getOptions for k6 test. 
 * @param {array} labels - array of labels to set thresholds for 
 * @returns 
 */
export function getOptions(labels) {
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
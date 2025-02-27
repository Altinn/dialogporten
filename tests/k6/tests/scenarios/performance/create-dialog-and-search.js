/**
 * Performance test for creating dialogs and searching dialogs.
 * Run: k6 run tests/k6/tests/scenarios/performance/create-dialog-and-search.js -e env=yt01 -e svus=1 -e evus=1 -e duration=1m
 */
import { randomItem } from '../../../common/k6-utils.js';
import { enduserSearch } from '../../performancetest_common/simpleSearch.js';
import { createDialog } from '../../performancetest_common/createDialog.js';
import { serviceOwners, endUsersWithTokens } from '../../performancetest_common/readTestdata.js';


/**
 * Options for performance scenarios.
 *
 * @typedef {Object} Options
 * @property {Object} scenarios - The performance scenarios.
 * @property {Object} summaryTrendStats - The summary trend statistics.
 * @property {Object} thresholds - The thresholds for performance metrics.
 */

/**
 * Performance options.
 *
 * @type {Options}
 */
export const options = {
    scenarios: {
      create_dialogs: {
        executor: 'constant-vus',
        tags: { name: 'create dialog'},
        exec: 'createDialogs',
        vus: __ENV.svus,
        duration: __ENV.duration
      },
      simple_search: {
        executor: 'constant-vus',
        tags: { name: 'search dialogs'},
        exec: 'enduserSearches',
        vus: __ENV.evus,
        duration: __ENV.duration,
      }
    },
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
      "http_req_duration{scenario:default}": [],
      "http_req_duration{name:enduser search}": [],
      "http_req_duration{name:create dialog}": [],
      "http_req_duration{name:get dialog}": [],
      "http_req_duration{name:get dialog activities}": [],
      "http_req_duration{name:get dialog activity}": [],
      "http_req_duration{name:get seenlogs}": [],
      "http_req_duration{name:get transmissions}": [],
      "http_req_duration{name:get transmission}": [],
      "http_req_duration{name:get labellog}": [],
      "http_reqs{scenario:default}": [],
      "http_reqs{name:enduser search}": [],
      "http_reqs{name:create dialog}": [],
      "http_reqs{name:get dialog activities}": [],
      "http_reqs{name:get dialog activity}": [],
      "http_reqs{name:get seenlogs}": [],
      "http_reqs{name:get transmissions}": [],
      "http_reqs{name:get transmission}": [],
      "http_reqs{name:get dialog}": [],
      "http_reqs{name:get labellog}": [],
  }

};

export function createDialogs() {
  if (!endUsersWithTokens || endUsersWithTokens.length === 0) {
    throw new Error('No end users loaded for testing');
  }
  if (!serviceOwners || serviceOwners.length === 0) {
    throw new Error('No service owners loaded for testing');
  }
  createDialog(randomItem(serviceOwners), randomItem(endUsersWithTokens));
}

export function enduserSearches() {
  if (!endUsersWithTokens || endUsersWithTokens.length === 0) {
    throw new Error('No end users loaded for testing');
  }
  enduserSearch(randomItem(endUsersWithTokens));
}

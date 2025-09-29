/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphql-search.js --vus 1 --iterations 1 -e env=yt01
 */
import { randomItem } from '../../../common/k6-utils.js';
import { getEndUserTokens } from '../../../common/token.js';
import { 
  getGraphqlRequestBodyForAllDialogsForParty, 
  getGraphqlRequestBodyForAllDialogsForCount,
  getPartiesRequestBody
} from '../../performancetest_data/graphql-queries.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { postGQ } from '../../../common/request.js';

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';
const defaultNumberOfEndUsers = (__ENV.NUMBER_OF_ENDUSERS ?? 2799);; // Max number of endusers from altinn-testtools now.
const breakpoint = (__ENV.BREAKPOINT ?? 'false') === 'true';
const abort_on_fail = (__ENV.ABORT_ON_FAIL ?? 'false') === 'true';
const stages_duration = __ENV.stages_duration ?? '5m';
const stages_target = __ENV.stages_target ?? 10;


export function log(json, enduser, duration) {
  if (!traceCalls) {
      return;
  }
  if (!json) {
      if (traceCalls) {
          console.log("No data in response for enduser " + enduser + (duration ? (" in " + duration + " ms") : ""));
      }
      return;
  }
  if (json.data?.searchDialogs?.items?.length) {
      console.log("Found " + json.data.searchDialogs.items.length + " dialogs" + " for " + enduser + (duration ? (" in " + duration + " ms") : "")); 
  } else if (json.data?.searchDialogs?.errors?.length) {
      console.log("Found errors " + JSON.stringify(json.data.searchDialogs.errors) + " for " + enduser + (duration ? (" in " + duration + " ms") : "")); 
  } else if (json.data?.parties?.length ) {
      console.log("Found " + json.data.parties.length + " parties" + " for " + enduser + (duration ? (" in " + duration + " ms") : ""));
  } else {
      console.log("Found no data for " + enduser + (duration ? (" in " + duration + " ms") : ""));
  }
}
/**
 * The options object for configuring the performance test for GraphQL search.
 *
 * @property {string[]} summaryTrendStats - The summary trend statistics to include in the test results.
 * @property {object} thresholds - The thresholds for the test metrics.
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
      options.thresholds[`http_reqs{name:${label}}`] = [{ threshold: 'rate<=0.0', abortOnFail: abort_on_fail }];
    }
    
    options.stages = [
      { duration: stages_duration, target: stages_target },
    ];
  } else {
    for (const label of labels) {
      options.thresholds[`http_req_duration{name:${label}}`] = [];
      options.thresholds[`http_reqs{name:${label}}`] = [];
    }
  }
  return options;
}


export function _setup(label, queryType, numberOfEndUsers = defaultNumberOfEndUsers) {
  const tokenOptions = {
    scopes: "digdir:dialogporten"
  }
  if (numberOfEndUsers === null) {
    numberOfEndUsers = defaultNumberOfEndUsers;
  }
  const endusers = getEndUserTokens(numberOfEndUsers, tokenOptions);
  return [label, queryType, endusers];
}

export function _default(data) {
  const label = data[0];
  const queryType = data[1];
  const endUser = randomItem(Object.keys(data[2]));
  const token = data[2][endUser];
  const paramsWithToken = {
    headers: {
      Authorization: "Bearer " + token,
      Accept: 'application/json',
      'User-Agent': 'dialogporten-k6',
    },
    tags: { name: label }
  }
  
  describe('Perform graphql dialog list', () => {
    let r = postGQ(createBody(endUser, queryType), paramsWithToken);
    expectStatusFor(r).to.equal(200);
    expect(r, 'response').to.have.validJsonBody();
    log(r.json(), endUser, r.timings.duration);
  });
}

export function _defaultForParties(data, all = true) {
  const parties_label = "graphql-get-parties";
  const dialogs_label = data[0];
  const queryType = data[1];
  const endUser = randomItem(Object.keys(data[2]));
  const token = data[2][endUser];
  const paramsWithToken = {
    headers: {
      Authorization: "Bearer " + token,
      Accept: 'application/json',
      'User-Agent': 'dialogporten-k6',
    },

  }
  paramsWithToken.tags = { name: parties_label }
  const parties = getParties(endUser, paramsWithToken, all);
  if (parties.length === 0) {
    console.warn(`No parties found for endUser ${endUser}, skipping test.`);
    return;
  }
  paramsWithToken.tags = { name: dialogs_label }
  
  describe('Perform graphql dialog list', () => {
    let r = postGQ(createBodyForMultiParties(parties, queryType), paramsWithToken);
    expectStatusFor(r).to.equal(200);
    expect(r, 'response').to.have.validJsonBody();
    const log_string = parties[0] + (parties.length > 1 ? "..." : "");
    log(r.json(), log_string, r.timings.duration);
  });
}

export function getParties(endUser, paramsWithToken, all) {  
  const queryType = "getParties"
  var response = {};
  describe('Get parties for enduser', () => {
    let r = postGQ(createBody(null, queryType), paramsWithToken);
    expectStatusFor(r).to.equal(200);
    expect(r, 'response').to.have.validJsonBody();
    log(r.json(), endUser, r.timings.duration);
    response = r;
  });
  return getPartiesFromResponse(response.json(), all);
}

function getPartiesFromResponse(json, all) {
  const parties = [];
  if (json.data && json.data.parties && json.data.parties.length > 0) {
    for (const party of json.data.parties) {
      if (party.isDeleted || !(party.party.includes("organization"))) continue;
      parties.push(party.party);

      for (const subParty of party.subParties) {
        if (subParty.isDeleted || !(subParty.party.includes("organization"))) continue;
        parties.push(subParty.party);
        if (parties.length >= 19) break; 
      }
      if (parties.length >= 19) break;
    }
  }
  if (all === false && parties.length > 0) {
    return [randomItem(parties)];
  }
  return parties;
}

export function createBody(endUser, type) {
  switch (type) {
    case "getAllDialogsForParty":
      return createBodyForAllDialogsForParty(endUser);
    case "getAllDialogsForCount":
      return createBodyForAllDialogsForCount(endUser);
    case "getParties":
      return createBodyForParties();
    default:
      return createBodyForAllDialogsForParty(endUser);
  }
}

export function createBodyForMultiParties(parties, type) {
  switch (type) {
    case "getAllDialogsForParties":
      return createBodyForAllDialogsForParties(parties);
    case "getAllDialogsForPartiesForCount":
      return createBodyForAllDialogsForPartiesForCount(parties);
    default:
      return createBodyForAllDialogsForParties(parties);
  }
}

function createBodyForAllDialogsForParties(parties) {
  const variables = {
    partyURIs: [...parties],
    limit: 100,
    label: ["DEFAULT"],
    status: ["NOT_APPLICABLE", "IN_PROGRESS", "AWAITING", "REQUIRES_ATTENTION", "COMPLETED"]
  }
  return getGraphqlRequestBodyForAllDialogsForParty(variables);
}

function createBodyForAllDialogsForPartiesForCount(parties, all) {
  const variables = {
    partyURIs: [...parties],
    limit: 1000,
  }
  return getGraphqlRequestBodyForAllDialogsForCount(variables);
}

function createBodyForAllDialogsForParty(endUser) {
  const variables = {
    partyURIs: [`urn:altinn:person:identifier-no:${endUser}`],
    limit: 100,
    label: ["DEFAULT"],
    status: ["NOT_APPLICABLE", "IN_PROGRESS", "AWAITING", "REQUIRES_ATTENTION", "COMPLETED"]
  }
  return getGraphqlRequestBodyForAllDialogsForParty(variables);
}

function createBodyForAllDialogsForCount(endUser) {
  const variables = {
    partyURIs: [`urn:altinn:person:identifier-no:${endUser}`],
    limit: 1000,
  }
  return getGraphqlRequestBodyForAllDialogsForCount(variables);
}

function createBodyForParties() {
  return getPartiesRequestBody();
}

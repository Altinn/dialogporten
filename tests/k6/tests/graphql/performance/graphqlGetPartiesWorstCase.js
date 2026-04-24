/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphqlGetPartiesWorstCase.js --vus 1 --iterations 30 -e env=yt01
 */

import { _default, getOptions, _setup, createBody, log } from './graphqlCommonFunctions.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { postGQ } from '../../../common/request.js';
import { getEnduserTokenFromGenerator } from '../../../common/token.js';

const environment = __ENV.API_ENVIRONMENT || "yt01";

// The label format is: label_pid_totalparties_partiesreturned, e.g. a_14022216091_84k_84k means 
// that the end user with pid 14022216091 has 84k parties, and all of them are returned by the query. 
const endUsersByEnvironment = {
  yt01: [
    { pid: "14022216091", label: "a_14022216091_84k_84k" },
    { pid: "21070450361", label: "b_21070450361_47k_1371" },
    { pid: "10121251049", label: "c_10121251049_30k_41" },
    { pid: "11111574113", label: "d_11111574113_27k_124" },
    { pid: "26091077719", label: "e_26091077719_27k_511" },
    { pid: "06053077178", label: "k_06053077178_5k_5k" },
    { pid: "03012995740", label: "l_03012995740_5k_5k" },
    { pid: "14063069966", label: "m_14063069966_5k_5k" },
    { pid: "21060469919", label: "q_21060469919_4k_4k" },
    { pid: "13031471499", label: "r_13031471499_4k_3k" },
  ],
  staging: [
    { pid: "06095101567", label: "a_06095101567_48k_11k" },
    { pid: "22877497392", label: "b_22877497392_16k_16k" },
    { pid: "05897398887", label: "c_05897398887_16k_16k" },
    { pid: "13886499404", label: "d_13886499404_13k_9" },
    { pid: "01055902352", label: "e_01055902352_12k_12k" },
  ],
  test: [
    { pid: "22877497392", label: "a_22877497392_15k_15k" },
    { pid: "13886499404", label: "b_13886499404_13k_13k" },
    { pid: "14836599080", label: "c_14836599080_6k_6k" },
    { pid: "23812849735", label: "d_23812849735_6k_6k" },
    { pid: "24916399592", label: "e_24916399592_6k_6k" },
  ],
};

const endUsers = endUsersByEnvironment[environment] || [];
const endUserLabels = endUsers.map(user => user.label);


const queryType = "getParties"

export const options = getOptions(endUserLabels);

export default function () {
  const user = endUsers[__ITER % endUsers.length];
  const endUser = user.pid;
  const label = user.label;
  const tokenOptions = {
    scopes: "digdir:dialogporten",
    ssn: endUser
  }
  const token = getEnduserTokenFromGenerator(tokenOptions);
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

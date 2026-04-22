/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphqlGetAllDialogsForParties.js --vus 1 --iterations 1 -e env=yt01
 */
import { getEnduserTokenFromGenerator } from '../../../common/token.js';
import { getOptions, _setup, _defaultForParties, log } from './graphqlCommonFunctions.js';
import { createBodyForMultiParties } from './graphqlCommonFunctions.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { postGQ } from '../../../common/request.js';
const queryType = "getAllDialogsForParties";
const environment = __ENV.API_ENVIRONMENT || "yt01";

const endUsersByEnvironment = {
  yt01: [
    { pid: "06917699338", label: "a_06917699338_73k" },
    { pid: "03905398104", label: "b_03905398104_66k" },
    { pid: "02845994504", label: "c_02845994504_54k" },
    { pid: "03836695584", label: "d_03836695584_42k" },
    { pid: "07926198712", label: "e_07926198712_34k" },
  ],
  staging: [
    { pid: "06917699338", label: "a_06917699338_73k" },
    { pid: "03905398104", label: "b_03905398104_66k" },
    { pid: "02845994504", label: "c_02845994504_54k" },
    { pid: "03836695584", label: "d_03836695584_42k" },
    { pid: "07926198712", label: "e_07926198712_34k" },
  ],
  test: [
    { pid: "06917699338", label: "a_06917699338_73k" },
    { pid: "03905398104", label: "b_03905398104_66k" },
    { pid: "02845994504", label: "c_02845994504_54k" },
    { pid: "03836695584", label: "d_03836695584_42k" },
    { pid: "07926198712", label: "e_07926198712_34k" },
  ],
};

const endUsers = endUsersByEnvironment[environment] || [];
const endUserLabels = endUsers.map(user => user.label);

export const options = getOptions(endUserLabels);

export default function () {
  const users = endUsersByEnvironment[environment];
  const endUser = users[__ITER % users.length].pid;
  const label = users[__ITER % users.length].label;

  const tokenOptions = {
    scopes: "digdir:dialogporten",
    ssn: endUser,
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
    const party = `urn:altinn:person:identifier-no:${endUser}`;
    let r = postGQ(createBodyForMultiParties([party], queryType), paramsWithToken);
    expectStatusFor(r).to.equal(200);
    expect(r, 'response').to.have.validJsonBody();
    log(r.json(), party, r.timings.duration);
  });
}
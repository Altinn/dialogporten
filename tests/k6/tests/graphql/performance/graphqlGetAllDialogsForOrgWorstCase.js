/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphqlGetAllDialogsForOrgWorstCase.js --vus 1 --iterations 15 -e env=yt01
 * TODO: Find real cases for staging and test environments, or remove those environments from the test. Currently using the same test data for all environments.
 */
import { getEnduserTokenFromGenerator } from '../../../common/token.js';
import { getOptions, _setup, _defaultForParties, log } from './graphqlCommonFunctions.js';
import { createBodyForMultiParties } from './graphqlCommonFunctions.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { postGQ } from '../../../common/request.js';
const queryType = "getAllDialogsForParties";
const environment = __ENV.API_ENVIRONMENT || "yt01";

// The label format is: label_orgno_totaldialogs, e.g. a_313274527_73k means that the orgno 313274527 has 73k dialogs.
// TODO: Find real cases for staging and test environments, or remove those environments from the test. Currently using the same test data for all environments.
const endUsersByEnvironment = {
  yt01: [
    { pid: "06917699338", orgno: "313274527", label: "a_313274527_73k" },
    { pid: "02916298334", orgno: "313110524", label: "b_313110524_65k" },
    { pid: "15917599510", orgno: "210331492", label: "c_210331492_56k" },
    { pid: "27886796175", orgno: "210684042", label: "d_210684042_46k" },
    { pid: "04857997919", orgno: "210696342", label: "e_210696342_35k" },
  ],
  staging: [
    { pid: "06917699338", orgno: "313274527", label: "a_313274527_73k" },
    { pid: "02916298334", orgno: "313110524", label: "b_313110524_65k" },
    { pid: "15917599510", orgno: "210331492", label: "c_210331492_56k" },
    { pid: "27886796175", orgno: "210684042", label: "d_210684042_46k" },
    { pid: "04857997919", orgno: "210696342", label: "e_210696342_35k" },
  ],
  test: [
    { pid: "06917699338", orgno: "313274527", label: "a_313274527_73k" },
    { pid: "02916298334", orgno: "313110524", label: "b_313110524_65k" },
    { pid: "15917599510", orgno: "210331492", label: "c_210331492_56k" },
    { pid: "27886796175", orgno: "210684042", label: "d_210684042_46k" },
    { pid: "04857997919", orgno: "210696342", label: "e_210696342_35k" },
  ],
};

const endUsers = endUsersByEnvironment[environment] || [];
const endUserLabels = endUsers.map(user => user.label);

export const options = getOptions(endUserLabels);

export default function () {
  const users = endUsersByEnvironment[environment];
  const endUser = users[__ITER % users.length].pid;
  const label = users[__ITER % users.length].label;
  const orgno = users[__ITER % users.length].orgno;

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
    const party = `urn:altinn:organization:identifier-no:${orgno}`;
    let r = postGQ(createBodyForMultiParties([party], queryType), paramsWithToken);
    expectStatusFor(r).to.equal(200);
    expect(r, 'response').to.have.validJsonBody();
    log(r.json(), party, r.timings.duration);
  });
}
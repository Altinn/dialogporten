/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphqlGetAllDialogsForOrgWorstCaseFTS.js --vus 1 --iterations 15 -e env=yt01
 * TODO: Find real cases for staging and test environments, or remove those environments from the test. Currently using the same test data for all environments.
 */
import { getEnduserTokenFromGenerator } from '../../../common/token.js';
import { getOptions, _setup, _defaultForParties, log } from './graphqlCommonFunctions.js';
import { createBodyForAllDialogsForPartiesFts } from './graphqlCommonFunctions.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { postGQ } from '../../../common/request.js';
const environment = __ENV.API_ENVIRONMENT || "yt01";

// The label format is: label_orgno_totaldialogs_searchterm, e.g. a_313274527_73k_dolor means that the orgno 313274527 has 73k dialogs and the search term is "dolor". 
// This is to test the worst case scenario for FTS, where the search term is very common and appears in many dialogs, which can potentially slow down the response time.
// TODO: Find real cases for staging and test environments, or remove those environments from the test. Currently using the same test data for all environments.
const endUsersByEnvironment = {
  yt01: [
    { pid: "06917699338", orgno: "213325612", label: "a_213325612_71k_dolor", word: "dolor" },
    { pid: "20824699322", orgno: "313315061", label: "b_313315061_68k_qui", word: "qui" },
    { pid: "09836597599", orgno: "313341437", label: "c_313341437_54k_aut", word: "aut" },
    { pid: "23915398146", orgno: "313910822", label: "d_313910822_43k_voluptatem", word: "voluptatem" },
    { pid: "16838698747", orgno: "311183753", label: "e_311183753_34k_quia", word: "quia" },
  ],
  staging: [
    { pid: "06917699338", orgno: "213325612", label: "a_213325612_71k_dolor", word: "dolor" },
    { pid: "20824699322", orgno: "313315061", label: "b_313315061_68k_qui", word: "qui" },
    { pid: "09836597599", orgno: "313341437", label: "c_313341437_54k_aut", word: "aut" },
    { pid: "23915398146", orgno: "313910822", label: "d_313910822_43k_voluptatem", word: "voluptatem" },
    { pid: "16838698747", orgno: "311183753", label: "e_311183753_34k_quia", word: "quia" },
  ],
  test: [
    { pid: "06917699338", orgno: "213325612", label: "a_213325612_71k_dolor", word: "dolor" },
    { pid: "20824699322", orgno: "313315061", label: "b_313315061_68k_qui", word: "qui" },
    { pid: "09836597599", orgno: "313341437", label: "c_313341437_54k_aut", word: "aut" },
    { pid: "23915398146", orgno: "313910822", label: "d_313910822_43k_voluptatem", word: "voluptatem" },
    { pid: "16838698747", orgno: "311183753", label: "e_311183753_34k_quia", word: "quia" },
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
  const searchWord = users[__ITER % users.length].word;

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
    let r = postGQ(createBodyForAllDialogsForPartiesFts([party], searchWord), paramsWithToken);
    expectStatusFor(r).to.equal(200);
    expect(r, 'response').to.have.validJsonBody();
    log(r.json(), party, r.timings.duration);
  });
}
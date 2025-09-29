/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphqlGetAllDialogsForParty.js --vus 1 --iterations 1 -e env=yt01
 */

import { getOptions, _setup, log, createBodyForAllDialogsForPartiesAutoCompleteFts, getParties } from './graphqlCommonFunctions.js';
import { getEndUserTokens } from '../../../common/token.js';
import { randomItem } from '../../../common/k6-utils.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { postGQ } from '../../../common/request.js';

const defaultNumberOfEndUsers = (__ENV.NUMBER_OF_ENDUSERS ?? 2799);

const parties_label = "graphql-get-parties";
const label1 = "autocomplete_off";
const label2 = "autocomplete_offi";
const label3 = "autocomplete_offici";
const label4 = "autocomplete_officia";
const labels = [parties_label, label1, label2, label3, label4];

export const options = getOptions(labels);

export function setup(numberOfEndUsers = defaultNumberOfEndUsers) {
  const tokenOptions = {
    scopes: "digdir:dialogporten"
  }
  if (numberOfEndUsers === null) {
    numberOfEndUsers = defaultNumberOfEndUsers;
  }
  const endusers = getEndUserTokens(numberOfEndUsers, tokenOptions);
  return endusers;
}

export default function(data) {
  const endUser = randomItem(Object.keys(data));
  const token = data[endUser];
  const paramsWithToken = {
    headers: {
      Authorization: "Bearer " + token,
      Accept: 'application/json',
      'User-Agent': 'dialogporten-k6',
    },
    tags: { name: parties_label }
  }

  const parties = getParties(endUser, paramsWithToken, true);
  if (parties.length === 0) {
    console.warn(`No parties found for endUser ${endUser}, skipping test.`);
    return;
  }

  paramsWithToken.tags = { name: label1 }
  search(parties, paramsWithToken, "off");
  paramsWithToken.tags = { name: label2 };
  search(parties, paramsWithToken, "offi",);
  paramsWithToken.tags = { name: label3 };
  search(parties, paramsWithToken, "offici");
  paramsWithToken.tags = { name: label4 };
  search(parties, paramsWithToken, "officia");

}

function search(parties, paramsWithToken, term) {  
  describe('Perform graphql dialog list', () => {
    let r = postGQ(createBodyForAllDialogsForPartiesAutoCompleteFts(parties, term), paramsWithToken);
    expectStatusFor(r).to.equal(200);
    expect(r, 'response').to.have.validJsonBody();
    const log_string = parties[0] + (parties.length > 1 ? ", ..." : "");
    log(r.json(), log_string, r.timings.duration);
  });
}

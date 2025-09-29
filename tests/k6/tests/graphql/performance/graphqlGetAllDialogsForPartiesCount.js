/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphqlGetAllDialogsForParty.js --vus 1 --iterations 1 -e env=yt01
 */
import { getOptions, _setup, _defaultForParties } from './graphqlCommonFunctions.js';
const dialogs_label = "graphql-getall-dialogs-for-parties-for-count";
const parties_label = "graphql-get-parties";
const queryType = "getAllDialogsForPartiesForCount"
const labels = [dialogs_label, parties_label];

export const options = getOptions(labels);
export function setup() { return _setup(dialogs_label, queryType); }
export default function (data) { _defaultForParties(data); } 
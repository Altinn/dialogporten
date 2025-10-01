/**
 * The performance test for GraphQL search.
 * Run: k6 run tests/k6/tests/graphql/performance/graphqlGetParties.js --vus 1 --iterations 1 -e env=yt01
 */

import { _default, getOptions, _setup } from './graphqlCommonFunctions.js';

const label = "graphql-get-parties";
const queryType = "getParties"

export const options = getOptions(label);
export function setup() { return _setup(label, queryType); }
export default function (data) { _default(data); }

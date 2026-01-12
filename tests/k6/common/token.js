import http from "k6/http";
import encoding from "k6/encoding";
import { extend } from "./extend.js";
import { defaultEndUserSsn, defaultServiceOwnerOrgNo, tokenGeneratorEnv, defaultSystemUserOrgNo, defaultSystemUserId} from "./config.js";

let defaultTokenOptionsForServiceOwner = {
  scopes: "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search",
  orgName: "ttd",
  orgNo: defaultServiceOwnerOrgNo
};

let defaultTokenOptionsForEndUser = {
    scopes: "digdir:dialogporten",
    ssn: defaultEndUserSsn,
    orgNo: defaultServiceOwnerOrgNo // a organzation number for a party that the end user has access to
};

let defaultTokenOptionsForSystemUser = {
    scopes: "digdir:dialogporten",
    systemUserId: defaultSystemUserId,
    systemUserOrg: defaultSystemUserOrgNo
}

const tokenUsername = __ENV.TOKEN_GENERATOR_USERNAME;
const tokenPassword = __ENV.TOKEN_GENERATOR_PASSWORD;

const tokenTtl = 3600;
const tokenMargin = 10;

const credentials = `${tokenUsername}:${tokenPassword}`;
const encodedCredentials = encoding.b64encode(credentials);
const tokenRequestOptions = {
  headers: {
    Authorization: `Basic ${encodedCredentials}`,
  },
  tags: {name: 'Token generator'},
};

let cachedTokens = {};
let cachedTokensIssuedAt = {};

function getCacheKey(tokenType, tokenOptions) {
  return `${tokenType}|${tokenOptions.scopes}|${tokenOptions.orgName}|${tokenOptions.orgNo}|${tokenOptions.ssn}|${tokenOptions.systemUserId}|${tokenOptions.systemUserOrg}`;
}

export function fetchToken(url, tokenOptions, type) {
  const currentTime = Math.floor(Date.now() / 1000);
  const cacheKey = getCacheKey(type, tokenOptions);

  if (!cachedTokens[cacheKey] || (currentTime - cachedTokensIssuedAt[cacheKey] >= tokenTtl - tokenMargin)) {
    if (__VU == 0) {
      console.info(`Fetching ${type} token from token generator during setup stage`);
    }
    else {
      console.info(`Fetching ${type} token from token generator during VU stage for VU #${__VU}`);
    }

    let response = http.get(url, tokenRequestOptions);

    if (response.status != 200) {
      throw new Error(`Failed getting ${type} token: ${response.status_text}`);
    }
    cachedTokens[cacheKey] = response.body;
    cachedTokensIssuedAt[cacheKey] = currentTime;
  }

  return cachedTokens[cacheKey];
}

export function getServiceOwnerTokenFromGenerator(tokenOptions = null) {
  let fullTokenOptions = extend({}, defaultTokenOptionsForServiceOwner, tokenOptions);
  const url = `http://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseToken?env=${tokenGeneratorEnv}&scopes=${encodeURIComponent(fullTokenOptions.scopes)}&org=${fullTokenOptions.orgName}&orgNo=${fullTokenOptions.orgNo}&ttl=${tokenTtl}`;
  return fetchToken(url, fullTokenOptions, `service owner (orgno:${fullTokenOptions.orgNo} orgName:${fullTokenOptions.orgName} tokenGeneratorEnv:${tokenGeneratorEnv})`);
}

export function getEnduserTokenFromGenerator(tokenOptions = null) {
  let fullTokenOptions = extend({}, defaultTokenOptionsForEndUser, tokenOptions);
  const url = `http://altinn-testtools-token-generator.azurewebsites.net/api/GetPersonalToken?env=${tokenGeneratorEnv}&scopes=${encodeURIComponent(fullTokenOptions.scopes)}&pid=${fullTokenOptions.ssn}&ttl=${tokenTtl}`;
  return fetchToken(url, fullTokenOptions, `end user (ssn:${fullTokenOptions.ssn}, tokenGeneratorEnv:${tokenGeneratorEnv})`);
}

export function getSystemUserTokenFromGenerator(tokenOptions = null) {
    let fullTokenOptions = extend({}, defaultTokenOptionsForSystemUser, tokenOptions);
    const url = `http://altinn-testtools-token-generator.azurewebsites.net/api/GetSystemUserToken?env=${tokenGeneratorEnv}&scopes=${encodeURIComponent(fullTokenOptions.scopes)}&systemUserId=${fullTokenOptions.systemUserId}&systemUserOrg=${fullTokenOptions.systemUserOrg}`;
    return fetchToken(url, fullTokenOptions, `system user (System user ID:${fullTokenOptions.systemUserId}, tokenGeneratorEnv:${tokenGeneratorEnv})`);
}

export function getEndUserTokens(count, tokenOptions = null) {
  let fullTokenOptions = extend({}, defaultTokenOptionsForEndUser, tokenOptions);
  const url = `http://altinn-testtools-token-generator.azurewebsites.net/api/GetPersonalToken?env=${tokenGeneratorEnv}&scopes=${encodeURIComponent(fullTokenOptions.scopes)}&bulkCount=${count}&ttl=${tokenTtl}`;
  tokenRequestOptions.timeout = 600000;
  console.log(url)
  let response = http.get(url, tokenRequestOptions);
  if (response.status != 200) {
    throw new Error(`Failed getting tokens: ${response.status_text}`);
  }
  return response.json();
}

export function getDefaultEnduserOrgNo() {
  return defaultTokenOptionsForEndUser.orgNo;
}

export function getDefaultEnduserSsn() {
  return defaultTokenOptionsForEndUser.ssn;
}

export function getDefaultServiceOwnerOrgNo() {
    return defaultServiceOwnerOrgNo;
}

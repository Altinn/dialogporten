import http from "k6/http";
import encoding from "k6/encoding";
import { extend } from "./extend.js";
import { defaultEndUserSsn, defaultServiceOwnerOrgNo, tokenGeneratorEnv } from "./config.js";

let defaultTokenOptions = {
  scopes: "digdir:dialogporten digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search",
  orgName: "digdir",
  orgNo: defaultServiceOwnerOrgNo,
  ssn: defaultEndUserSsn
};

const tokenUsername = __ENV.TOKEN_GENERATOR_USERNAME;
const tokenPassword = __ENV.TOKEN_GENERATOR_PASSWORD;

const tokenTtl = 3600;
const tokenMargin = 10;

const credentials = `${tokenUsername}:${tokenPassword}`;
const encodedCredentials = encoding.b64encode(credentials);
const tokenRequestOptions = {
  headers: {
    Authorization: `Basic ${encodedCredentials}`,
  }
};

let cachedTokens = {};
let cachedTokensIssuedAt = {};

function getCacheKey(tokenType, tokenOptions) {
  return `${tokenType}|${tokenOptions.scopes}|${tokenOptions.orgName}|${tokenOptions.orgNo}|${tokenOptions.ssn}`;
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
  let fullTokenOptions = extend({}, defaultTokenOptions, tokenOptions);
  const url = `http://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseToken?env=${tokenGeneratorEnv}&scopes=${encodeURIComponent(fullTokenOptions.scopes)}&org=${fullTokenOptions.orgName}&orgNo=${fullTokenOptions.orgNo}&ttl=${tokenTtl}`;
  return fetchToken(url, fullTokenOptions, `service owner (orgno:${fullTokenOptions.orgNo} orgName:${fullTokenOptions.orgName} tokenGeneratorEnv:${tokenGeneratorEnv})`);
}

export function getEnduserTokenFromGenerator(tokenOptions = null) {
  let fullTokenOptions = extend({}, defaultTokenOptions, tokenOptions);
  const url = `http://altinn-testtools-token-generator.azurewebsites.net/api/GetPersonalToken?env=${tokenGeneratorEnv}&scopes=${encodeURIComponent(fullTokenOptions.scopes)}&pid=${fullTokenOptions.ssn}&ttl=${tokenTtl}`;
  return fetchToken(url, fullTokenOptions, `end user (ssn:${fullTokenOptions.ssn}, tokenGeneratorEnv:${tokenGeneratorEnv})`);
}

export function getEndUserTokens(count, tokenOptions = null) {
  let fullTokenOptions = extend({}, defaultTokenOptions, tokenOptions);
  const url = `http://altinn-testtools-token-generator.azurewebsites.net/api/GetPersonalToken?env=${tokenGeneratorEnv}&scopes=${encodeURIComponent(fullTokenOptions.scopes)}&bulkCount=${count}&ttl=${tokenTtl}`;
  tokenRequestOptions.timeout = 600000;
  let response = http.get(url, tokenRequestOptions);
  if (response.status != 200) {
    throw new Error(`Failed getting tokens: ${response.status_text}`);
  }
  return response.json();
}

export function getDefaultEnduserOrgNo() {
  return defaultTokenOptions.orgNo;
}

export function getDefaultEnduserSsn() {
  return defaultTokenOptions.ssn;
}

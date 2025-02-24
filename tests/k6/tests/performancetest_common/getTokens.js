import { fetchToken } from "../../common/token.js";

const tokenGeneratorEnv = __ENV.API_ENVIRONMENT || 'yt01';
const tokenTtl = __ENV.TTL || 3600;

export function getEnterpriseToken(serviceOwner) {
    const tokenOptions = {
        scopes: serviceOwner.scopes,
        orgName: serviceOwner.org,
        orgNo: serviceOwner.orgno
    }
    const url = `https://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseToken?env=${tokenGeneratorEnv}&scopes=${encodeURIComponent(tokenOptions.scopes)}&org=${tokenOptions.orgName}&orgNo=${tokenOptions.orgNo}&ttl=${tokenTtl}`;
    return fetchToken(url, tokenOptions, `service owner (orgno:${tokenOptions.orgNo} orgName:${tokenOptions.orgName} tokenGeneratorEnv:${tokenGeneratorEnv})`);
}

export function getPersonalToken(endUser) {
    const tokenOptions = {
        scopes: endUser.scopes,
        ssn: endUser.ssn
    }
    const url = `https://altinn-testtools-token-generator.azurewebsites.net/api/GetPersonalToken?env=${tokenGeneratorEnv}&scopes=${encodeURIComponent(tokenOptions.scopes)}&pid=${tokenOptions.ssn}&ttl=${tokenTtl}`;
    return fetchToken(url, tokenOptions, `end user (ssn:${tokenOptions.ssn}, tokenGeneratorEnv:${tokenGeneratorEnv})`);
  }

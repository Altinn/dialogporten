import { default as http } from 'k6/http';
import { baseUrlEndUser, baseUrlGraphql, baseUrlServiceOwner } from './config.js'
import { getServiceOwnerTokenFromGenerator, getEnduserTokenFromGenerator } from './token.js'
import { extend } from './extend.js'

function resolveParams(defaultParams, params) {
    if (params == null) return defaultParams;
    let fullParams = extend(true, {}, defaultParams, params);
    // Header values set to null in params indicate that they should be removed
    for (let header in fullParams.headers) {
        if (fullParams.headers[header] === null) {
            delete fullParams.headers[header];
        }
    }
    return fullParams;
}

function getServiceOwnerRequestParams(params = null, tokenOptions = null) {

    params = params || {};
    const headers = params.headers || {};
    const hasOverridenAuthorizationHeader = headers.Authorization !== undefined;

    const defaultParams = {
        headers: {
            'Accept': 'application/json',
            'User-Agent': 'dialogporten-k6',
            'Authorization': hasOverridenAuthorizationHeader ? headers.Authorization : 'Bearer ' + getServiceOwnerTokenFromGenerator(tokenOptions)
        }
    }

    return resolveParams(defaultParams, params);
}

function getEnduserRequestParams(params = null, tokenOptions = null) {
    params = params || {};
    const headers = params.headers || {};
    const hasOverridenAuthorizationHeader = headers.Authorization !== undefined;
    let defaultParams = {
        headers: {
            'Accept': 'application/json',
            'User-Agent': 'dialogporten-k6',
            'Authorization': hasOverridenAuthorizationHeader ? headers.Authorization : 'Bearer ' + getEnduserTokenFromGenerator(tokenOptions)
        }
    }

    return resolveParams(defaultParams, params);
}

function maybeStringifyBody(maybeObject) {
    if (typeof maybeObject !== "string") {
        return JSON.stringify(maybeObject);
    }

    return maybeObject;
}

export function getSO(url, params = null, tokenOptions = null) {
    return http.get(baseUrlServiceOwner + url, getServiceOwnerRequestParams(params, tokenOptions))
}

export function postSO(url, body, params = null, tokenOptions = null) {
    body = maybeStringifyBody(body);
    params = extend(true, {}, params, { headers: { 'Content-Type': 'application/json' }});
    return http.post(baseUrlServiceOwner + url, body, getServiceOwnerRequestParams(params, tokenOptions));
}

export function postBatchSO(batch) {
    const processedBatch = batch.map(([url, body, params]) => {
        params = extend(true, {}, params, { headers: { 'Content-Type': 'application/json' }});
        return ['POST', baseUrlServiceOwner + url, JSON.stringify(body), getServiceOwnerRequestParams(params)];
    });

    return http.batch(processedBatch);
}

export function postSOAsync(url, body, params = null, tokenOptions = null) {
    body = maybeStringifyBody(body);
    params = extend(true, {}, params, { headers: { 'Content-Type': 'application/json' }});
    return http.asyncRequest('POST', baseUrlServiceOwner + url, body, getServiceOwnerRequestParams(params, tokenOptions));
}

export function putSO(url, body, params = null, tokenOptions = null) {
    body = maybeStringifyBody(body);
    params = extend(true, {}, params, { headers: { 'Content-Type': 'application/json' }});
    return http.put(baseUrlServiceOwner + url, body, getServiceOwnerRequestParams(params, tokenOptions));
}

export function patchSO(url, body, params = null, tokenOptions = null) {
    body = maybeStringifyBody(body);
    params = extend(true, {}, params, { headers: { 'Content-Type': 'application/json' }});
    return http.patch(baseUrlServiceOwner + url, body, getServiceOwnerRequestParams(params, tokenOptions));
}

export function deleteSO(url, params = null, tokenOptions = null) {
    return http.request('DELETE', baseUrlServiceOwner + url, {}, getServiceOwnerRequestParams(params, tokenOptions));
}

export function purgeSO(url, params = null, tokenOptions = null) {
    return http.request('POST', baseUrlServiceOwner + url + "/actions/purge", {}, getServiceOwnerRequestParams(params, tokenOptions));
}

export function freezeSO(url, params = null, tokenOptions = null) {
    return http.request('POST', baseUrlServiceOwner + url + "/actions/freeze", {}, getServiceOwnerRequestParams(params, tokenOptions));
}

export function getEU(url, params = null, tokenOptions = null) {
    return http.get(baseUrlEndUser + url, getEnduserRequestParams(params, tokenOptions))
}

export function postEU(url, body, params = null, tokenOptions = null) {
    body = maybeStringifyBody(body);
    params = extend(true, {}, params, { headers: { 'Content-Type': 'application/json' }});
    return http.post(baseUrlEndUser + url, body, getEnduserRequestParams(params, tokenOptions));
}

export function putEU(url, body, params = null, tokenOptions = null) {
    body = maybeStringifyBody(body);
    params = extend(true, {}, params, { headers: { 'Content-Type': 'application/json' }});
    return http.put(baseUrlEndUser + url, body, getEnduserRequestParams(params, tokenOptions));
}

export function patchEU(url, body, params = null, tokenOptions = null) {
    body = maybeStringifyBody(body);
    params = extend(true, {}, params, { headers: { 'Content-Type': 'application/json' }});
    return http.patch(baseUrlEndUser + url, body, getEnduserRequestParams(params, tokenOptions));
}

export function deleteEU(url, params = null, tokenOptions = null) {
    return http.request('DELETE', baseUrlEndUser + url, getEnduserRequestParams(params, tokenOptions));
}

export function postGQ(body, params = null) {
    body = JSON.stringify(body)
    params = extend(true, {}, params, { headers: { 'Content-Type': 'application/json' }});
    return http.post(baseUrlGraphql, body, params);
}

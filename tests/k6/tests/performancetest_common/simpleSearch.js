/**
 * This file contains common functions for performing simple searches
 * and GraphQL searches.
 */
import { randomItem, uuidv4 } from '../../common/k6-utils.js';
import { expect, expectStatusFor } from "../../common/testimports.js";
import { describe } from '../../common/describe.js';
import { getEU, getSO } from '../../common/request.js';
import { getEnterpriseToken, getPersonalToken } from './getTokens.js';

export const emptySearchThresholds = {
    "http_req_duration{scenario:default}": [],
    "http_req_duration{name:get dialog}": [],
    "http_req_duration{name:get dialog activities}": [],
    "http_req_duration{name:get dialog activity}": [],
    "http_req_duration{name:get seenlogs}": [],
    "http_req_duration{name:get seenlog}": [],
    "http_req_duration{name:get transmissions}": [],
    "http_req_duration{name:get transmission}": [],
    "http_reqs{scenario:default}": [],
    "http_reqs{name:get dialog activities}": [],
    "http_reqs{name:get dialog activity}": [],
    "http_reqs{name:get seenlogs}": [],
    "http_reqs{name:get seenlog}": [],
    "http_reqs{name:get transmissions}": [],
    "http_reqs{name:get transmission}": [],
    "http_reqs{name:get dialog}": [],
}

/**
 * Retrieves the content for a dialog.
 * Get dialog, dialog activities, seenlogs, labellog, and transmissions.
 * @param {Object} response - The response object.
 * @param {Object} paramsWithToken - The parameters with token.
 * @returns {void}
 */
function retrieveDialogContent(response, paramsWithToken, getFunction = getEU) {
    const items = response.json().items;
    if (!items?.length) return;
    const dialogId = items[0].id;
    if (!dialogId) return;
    if (getFunction == getSO) {
      const r = getDialog(dialogId, paramsWithToken, 'get dialog', getFunction);
      if (r != 200) {
          console.warn(`Dialog ${dialogId} not found for serviceowner.`);
          return;
      }
    }
    else {
      getContent(dialogId, paramsWithToken, 'get dialog', '', getFunction);
    }
    getContentChain(dialogId, paramsWithToken, 'get dialog activities', 'get dialog activity', '/activities/', getFunction);
    getContentChain(dialogId, paramsWithToken, 'get seenlogs', 'get seenlog', '/seenlog/', getFunction);
    if (getFunction == getEU) {
        getContent(dialogId, paramsWithToken, 'get labellog', '/context/labellog', getFunction);
    }
    getContentChain(dialogId, paramsWithToken, 'get transmissions', 'get transmission', '/transmissions/', getFunction);
}

export function log(items, traceCalls, enduser, duration = null) {
    if (items?.length && traceCalls) {
        console.log("Found " + items.length + " dialogs" + " for enduser " + enduser + (duration ? (" in " + duration + " ms") : ""));  
    } else if (traceCalls) {
        console.log("Found no dialogs for enduser " + enduser + (duration ? (" in " + duration + " ms") : ""));
    }
}

/**
 * Performs a enduser search.
 * @param {Object} enduser - The end user.
 * @returns {void}
 */
export function enduserSearch(enduser, token, traceCalls) {
    if (token == null) {
        token = getPersonalToken({ ssn: enduser, scopes: "digdir:dialogporten" });
    }
    var traceparent = uuidv4();
    let paramsWithToken = {
        headers: {
            Authorization: "Bearer " + token,
            traceparent: traceparent
        },
        tags: { name: 'enduser search' }
    }
    if (traceCalls) {
        paramsWithToken.tags.traceparent = traceparent;
        paramsWithToken.tags.enduser = enduser.ssn;
    }
    let defaultParty = "urn:altinn:person:identifier-no:" + enduser;
    let defaultFilter = "?Party=" + defaultParty
    describe('Perform enduser dialog list', () => {
        let r = getEU('dialogs' + defaultFilter, paramsWithToken);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        retrieveDialogContent(r, paramsWithToken);
        log(r.json().items, traceCalls, enduser);
    });
}

/**
 * Performs a serviceowner get dialog.
 * @param {string} dialogId - The dialog id.
 * @param {Object} paramsWithToken - The parameters with token.
 * @param {string} tag - Tagging the request.
 * @param {string} path - The path to append to the URL. Can be empty or /context/labellog.
 * @param {function} getFunction - The get function to use.
 * @returns {void}
 */
export function getDialog(dialogId, paramsWithToken, tag, getFunction = getSO) {
    const listParams = {
        ...paramsWithToken,
        tags: { ...paramsWithToken.tags, name: tag }
    };
    const url = 'dialogs/' + dialogId;
    let r = getFunction(url, listParams);
    expect(r.status, 'response status').to.be.oneOf([200, 404]);
    expect(r, 'response').to.have.validJsonBody();
    return r.status;
  
}

/**
 * Performs a enduser or serviceowner search.
 * @param {string} dialogId - The dialog id.
 * @param {Object} paramsWithToken - The parameters with token.
 * @param {string} tag - Tagging the request.
 * @param {string} path - The path to append to the URL. Can be empty or /context/labellog.
 * @param {function} getFunction - The get function to use.
 * @returns {void}
 */
export function getContent(dialogId, paramsWithToken, tag, path = '', getFunction = getEU) {
    const listParams = {
        ...paramsWithToken,
        tags: { ...paramsWithToken.tags, name: tag }
    };
    getUrl('dialogs/' + dialogId + path, listParams, getFunction);
}

/**
 * Retrieves the content chain.
 * @param {string} dialogId - The dialog id.
 * @param {Object} paramsWithToken - The parameters with token.
 * @param {string} tag - Tagging the request.
 * @param {string} subtag - Tagging the sub request.
 * @param {string} endpoint - The endpoint to append to the URL.
 * @param {function} getFunction - The get function to use.
 * @returns {void}
 */
export function getContentChain(dialogId, paramsWithToken, tag, subtag, endpoint, getFunction = getEU) {
    const listParams = {
        ...paramsWithToken,
        tags: { ...paramsWithToken.tags, name: tag }
    };
    let d = getUrl('dialogs/' + dialogId + endpoint, listParams, getFunction);
    let json = d.json();
    if (json.length > 0) {
        const detailParams = {
            ...paramsWithToken,
            tags: { ...paramsWithToken.tags, name: subtag }
        };
        getUrl('dialogs/' + dialogId + endpoint + randomItem(json).id, detailParams, getFunction);
    }
}

/**
 * Performs a GET request to the specified URL with the provided parameters.
 * @param {string} url - The URL to send the GET request to.
 * @param {Object} paramsWithToken - The parameters with token.
 * @param {function} getFunction - The get function to use.
 * @returns {Object} The response object.
 */
export function getUrl(url, paramsWithToken, getFunction = getEU) {
    let r = getFunction(url, paramsWithToken);
    if (r.status != 200) {
        console.error(`Failed to get ${url}: ${r.status} - ${r.body}`);
    }
    expectStatusFor(r).to.equal(200);
    expect(r, 'response').to.have.validJsonBody();
    return r;
}

/**
 * Performs a serviceowner search.
 * @param {P} serviceowner
 * @param {*} enduser
 * @param {*} tag_name
 */
export function serviceownerSearch(serviceowner, enduser, tag_name, traceCalls, doSubqueries = true) {
    let traceparent = uuidv4();
    let paramsWithToken = {
        headers: {
            Authorization: "Bearer " + getEnterpriseToken(serviceowner),
            traceparent: traceparent
        },
        tags: { name: tag_name }
    }

    if (traceCalls) {
        paramsWithToken.tags.traceparent = traceparent;
    }

    let enduserid = encodeURIComponent(`urn:altinn:person:identifier-no:${enduser.ssn}`);
    let serviceResource = encodeURIComponent(`urn:altinn:resource:${enduser.resource}`);
    let defaultFilter = `?enduserid=${enduserid}&serviceResource=${serviceResource}`;
    describe('Perform serviceowner dialog list', () => {
        let r = getSO('dialogs' + defaultFilter, paramsWithToken);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        if (doSubqueries) {
            retrieveDialogContent(r, paramsWithToken, getSO);
        }
        log(r.json().items, traceCalls, enduser);
        return r
    });
}

/**
 * Performs a serviceowner party  search.
 * @param {P} serviceowner
 * @param {*} enduser
 * @param {*} tag_name
 */
export function serviceownerPartySearch(serviceowner, party, tag_name, traceCalls, doSubqueries = true) {
  let traceparent = uuidv4();
  let paramsWithToken = {
      headers: {
          Authorization: "Bearer " + getEnterpriseToken(serviceowner),
          traceparent: traceparent
      },
      tags: { name: tag_name }
  }

  if (traceCalls) {
      paramsWithToken.tags.traceparent = traceparent;
  }

  let partyid = encodeURIComponent(`urn:altinn:organization:identifier-no:${party.partyId}`);
  let defaultFilter = `?party=${partyid}`;
  describe('Perform serviceowner dialog list', () => {
      let r = getSO('dialogs' + defaultFilter, paramsWithToken);
      expectStatusFor(r).to.equal(200);
      expect(r, 'response').to.have.validJsonBody();
      if (doSubqueries) {
          retrieveDialogContent(r, paramsWithToken, getSO);
      }
      log(r.json().items, traceCalls, party.partyId);
      return r
  });
}

/**
 * Common functions for creating dialogs.
 */
import { uuidv4 } from '../../common/k6-utils.js';
import { describe } from "../../common/describe.js";
import { postSO, purgeSO } from "../../common/request.js";
import { expect } from "../../common/testimports.js";
import dialogToInsert from "../performancetest_data/01-create-dialog.js";
import { default as transmissionToInsert } from "../performancetest_data/create-transmission.js";
import { getEnterpriseToken } from "./getTokens.js";

/**
 * Creates a dialog.
 *
 * @param {Object} serviceOwner - The service owner object.
 * @param {Object} endUser - The end user object.
 */

export function createDialog(serviceOwner, endUser, traceCalls) {
    var traceparent = uuidv4();

    var paramsWithToken = {
        headers: {
            Authorization: "Bearer " + getEnterpriseToken(serviceOwner),
            traceparent: traceparent
        },
        tags: { name: 'create dialog' }
    };

    if (traceCalls) {
        paramsWithToken.tags.traceparent = traceparent;
        paramsWithToken.tags.enduser = endUser.ssn;
    }

    describe('create dialog', () => {
        let r = postSO('dialogs', dialogToInsert(endUser.ssn, endUser.resource), paramsWithToken);
        expect(r.status, 'response status').to.equal(201);
    });
}

/**
 * Creates a dialog and removes it.
 *
 * @param {Object} serviceOwner - The service owner object.
 * @param {Object} endUser - The end user object.
 */
export function createAndRemoveDialog(serviceOwner, endUser, traceCalls) {
    var traceparent = uuidv4();
    var paramsWithToken = {
        headers: {
            Authorization: "Bearer " + getEnterpriseToken(serviceOwner),
            traceparent: traceparent
        },
        tags: { name: 'create dialog' }
    }
    if (traceCalls) {
        paramsWithToken.tags.traceparent = traceparent;
        paramsWithToken.tags.enduser = endUser.ssn;
    }

    let dialogId = 0;
    describe('create dialog', () => {
      paramsWithToken.tags.name = 'create dialog';
      let r = postSO('dialogs', dialogToInsert(endUser.ssn, endUser.resource), paramsWithToken);
      expect(r.status, 'response status').to.equal(201);
      dialogId = r.json();
    });

    describe('remove dialog', () => {
      traceparent = uuidv4();
      paramsWithToken.tags.name = 'remove dialog';
      if (dialogId) {
          let r = purgeSO('dialogs/' + dialogId, paramsWithToken);
          expect(r.status, 'response status').to.equal(204);
      }
  });
}

/**
 * Creates a dialog and add a number of transmissions
 *
 * @param {Object} serviceOwner - The service owner object.
 * @param {Object} endUser - The end user object.
 */
export function createTransmissions(serviceOwner, endUser, traceCalls, numberOfTransmissions, maxTransmissionsInThread, testid) {
    let traceparent = uuidv4();

    let paramsWithToken = {
        headers: {
            Authorization: "Bearer " + getEnterpriseToken(serviceOwner),
            traceparent: traceparent
        },
        tags: { name: 'create dialog', testid: testid }
    };
    if (traceCalls) {
        paramsWithToken.tags.traceparent = traceparent;
        paramsWithToken.tags.enduser = endUser.ssn;
    }

    let dialogId = 0;
    describe('create dialog', () => {
        let r = postSO('dialogs', dialogToInsert(endUser.ssn, endUser.resource), paramsWithToken);
        dialogId = r.json();
        expect(r.status, 'response status').to.equal(201);
    });

    let relatedTransmissionId = 0;
    for (let i = 0; i < numberOfTransmissions; i++) {

        relatedTransmissionId = createTransmission(dialogId, relatedTransmissionId, serviceOwner, traceCalls, testid);
        // Max transmissions in thread reached, start new thread
        if (i%maxTransmissionsInThread === 0) {
            relatedTransmissionId = 0;
        }
    }

}

export function createTransmission(dialogId, relatedTransmissionId, serviceOwner, traceCalls, testid) {
    let traceparent = uuidv4();

    let paramsWithToken = {
        headers: {
            Authorization: "Bearer " + getEnterpriseToken(serviceOwner),
            traceparent: traceparent
        },
        tags: { name: 'create transmission', testid: testid }
    };

    let newRelatedTransmissionId;
    describe('create transmission', () => {
        let r = postSO('dialogs/' + dialogId + '/transmissions', transmissionToInsert(relatedTransmissionId), paramsWithToken);
        expect(r.status, 'response status').to.equal(201);
        newRelatedTransmissionId = r.json();
    });
    return newRelatedTransmissionId;
}

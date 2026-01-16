/**
 * Common functions for creating dialogs.
 */
import { uuidv4 } from '../../common/k6-utils.js';
import { describe } from "../../common/describe.js";
import { postSO, purgeSO } from "../../common/request.js";
import { expect } from "../../common/testimports.js";
import dialogToInsert from "../performancetest_data/01-create-dialog.js";
import { default as transmissionToInsert, transmissionToInsertSkd } from "../performancetest_data/create-transmission.js";
import { getEnterpriseToken } from "./getTokens.js";
import { uuidv7 } from "../../common/uuid.js";


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

/**
 * Creates a dialog and add a transmission and an activity
 *
 * @param {Object} serviceOwner - The service owner object.
 * @param {Object} endUser - The end user object.
 * @param {boolean} traceCalls - Whether to trace calls.
 * @param {string} testid - The test identifier.
 * @return {Array} - An array containing the dialogId, relatedTransmissionId and activityId.
 */
export function CreateDialogTransmissionAndActivity(serviceOwner, endUser, traceCalls, testid) {
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
  const removeTransmissionsAndActivities = true;
  describe('create dialog', () => {
      let r = postSO('dialogs', dialogToInsert(endUser.ssn, endUser.resource, removeTransmissionsAndActivities), paramsWithToken);
      dialogId = r.json();
      expect(r.status, 'response status').to.equal(201);
  });

  const relatedTransmissionId = createTransmission(dialogId, 0, serviceOwner, testid, true);
  const activityId = createActivity(dialogId, serviceOwner, testid);
  return [dialogId, relatedTransmissionId, activityId];
}

/**
 * Creates a transmission.
 * @param {uuidv7} dialogId -  The dialogId to add the transmission to.
 * @param {uuidv7} relatedTransmissionId - The relatedTransmissionId for threading.
 * @param {object} serviceOwner - holds service owner information.
 * @param {uuidv7} testid - The test identifier. 
 * @param {boolean} skdTransmission - Whether to create a SKD transmission or not. Default is false. 
 * @return {uuidv7} - The id of the created transmission.
 */
export function createTransmission(dialogId, relatedTransmissionId, serviceOwner, testid, skdTransmission = false) {
    let traceparent = uuidv4();

    let paramsWithToken = {
        headers: {
            Authorization: "Bearer " + getEnterpriseToken(serviceOwner),
            traceparent: traceparent
        },
        tags: { name: 'create transmission', testid: testid }
    };

    let transmission = undefined;
    if (skdTransmission) {
        transmission = transmissionToInsertSkd(relatedTransmissionId, serviceOwner.orgno);
    } else {
        transmission = transmissionToInsert(relatedTransmissionId);
    }

    let newRelatedTransmissionId;
    describe('create transmission', () => {
        let r = postSO('dialogs/' + dialogId + '/transmissions', transmission, paramsWithToken);
        if (r.status !== 201) {
            console.log('Failed to create transmission. Status: ' + r.status + ', body: ' + r.body);
        }
        expect(r.status, 'response status').to.equal(201);
        newRelatedTransmissionId = r.json();
    });
    return newRelatedTransmissionId;
}

/**
 * Create an activity
 * @param {uuidv7} dialogId - The dialogId to add the activity to. 
 * @param {*} serviceOwner - holds service owner information.
 * @param {*} testid - The test identifier.
 * @return {uuidv7} - The id of the created activity.
 */

function createActivity(dialogId, serviceOwner, testid) {
    let traceparent = uuidv4();

    let paramsWithToken = {
        headers: {
            Authorization: "Bearer " + getEnterpriseToken(serviceOwner),
            traceparent: traceparent
        },
        tags: { name: 'create activity', testid: testid }
    };

    let activityId;
    describe('create activity', () => {
        let r = postSO(`dialogs/${dialogId}/activities`, getActivityBody(), paramsWithToken);
        expect(r.status, 'response status').to.equal(201);
        activityId = r.json();
    });
    return activityId;
} 

/**
 * Get activity body
 * @return {Object} - The activity body.
 */
function getActivityBody() 
{
  return {
    "id": uuidv7(),
    "transmissionId": null,
    "extendedType": "string",
    "performedBy": {
      "actorType": "ServiceOwner",
      "actorId": null,
      "actorName": null
    },
    "description": [],
    "type": "DialogCreated",
    "createdAt": new Date().toISOString()
  };
}

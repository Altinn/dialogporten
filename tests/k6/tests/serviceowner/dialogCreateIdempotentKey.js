import {describe, expect, expectStatusFor, postSO, purgeSO, uuidv7} from '../../common/testimports.js'
import {default as dialogToInsert} from './testdata/01-create-dialog.js';
import { otherOrgName, otherOrgNo, otherServiceResource } from '../../common/config.js';

export default function () {

    const dialogs = [];
    const otherOrg = {
        orgName: otherOrgName,
        orgNo: otherOrgNo,
    };

    describe('Attempt to create dialog with unused idempotentKey', () => {
        let dialog = dialogToInsert();
        dialog.idempotentKey = uuidv7();
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);

        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

        dialogs.push({
            id: r.json(),
            org: null
        });
    })

    describe('Attempt to create dialog with same idempotentKey for a different org code', () => {
        // idempotentKey uniqueness is scoped by the org code of the resource owner, so the same key
        // must be accepted once per org code. otherServiceResource is owned by a different org code
        // than the default resource.
        const idempotentKey = uuidv7();

        let dialog = dialogToInsert();
        dialog.idempotentKey = idempotentKey;
        dialog.serviceResource = "urn:altinn:resource:" + otherServiceResource;
        dialog.activities[2].performedBy.actorId = "urn:altinn:organization:identifier-no:" + otherOrgNo;

        let responseOtherOrg = postSO('dialogs', dialog, null, otherOrg);
        expectStatusFor(responseOtherOrg).to.equal(201);

        expect(responseOtherOrg, 'response').to.have.validJsonBody();
        expect(responseOtherOrg.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

        const otherOrgDialogId = responseOtherOrg.json();
        dialogs.push({
            id: otherOrgDialogId,
            org: otherOrg
        })

        dialog = dialogToInsert();
        dialog.idempotentKey = idempotentKey;

        let responseDefaultOrg = postSO('dialogs', dialog);
        expectStatusFor(responseDefaultOrg).to.equal(201);

        expect(responseDefaultOrg, 'response').to.have.validJsonBody();
        expect(responseDefaultOrg.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);
        expect(responseDefaultOrg.json(), 'response json').to.not.equal(otherOrgDialogId);

        dialogs.push({
            id: responseDefaultOrg.json(),
            org: null
        });
    })

    describe('Attempt to create dialog with used idempotentKey', () => {
        let dialog = dialogToInsert();
        dialog.idempotentKey = uuidv7();
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);

        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

        let dialogId = r.json();
        dialogs.push({
            id: dialogId,
            org: null
        });

        r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(409);

        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.property('errors');
        expect(r.json()['errors'], 'response json errors').to.property('IdempotentKey');
        expect(r.json()['errors']['IdempotentKey'][0], 'response json Conflict').to.contain(dialogId);
    })

    describe('Attempt to create dialog with too long idempotentKey', () => {
        let dialog = dialogToInsert();
        dialog.idempotentKey = "this idempotent id is way to long the length of this idempotent id exceeds the 36 character limit";

        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(400);

        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property('errors');

    })

    describe('Cleanup', () => {
        let i;
        for (i = 0; i < dialogs.length; i++) {
            let r = purgeSO('dialogs/' + dialogs[i].id, null, dialogs[i].org);
            expectStatusFor(r).to.equal(204);
        }
        expect(dialogs.length).to.equal(i);
    });
}

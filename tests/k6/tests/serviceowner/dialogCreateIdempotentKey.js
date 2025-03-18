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

    describe('Attempt to create dialog with same idempotentKey different Org', () => {
        let dialog = dialogToInsert();
        dialog.idempotentKey = uuidv7();
        dialog.serviceResource = "urn:altinn:resource:" +otherServiceResource;
        dialog.activities[2].performedBy.actorId = "urn:altinn:organization:identifier-no:" + otherOrgNo;

        let responseNav = postSO('dialogs', dialog, null, otherOrg);
        expectStatusFor(responseNav).to.equal(201);

        expect(responseNav, 'response').to.have.validJsonBody();
        expect(responseNav.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

        dialogs.push({
            id: responseNav.json(),
            org: otherOrg
        })

        dialog = dialogToInsert();
        dialog.idempotentKey = uuidv7();

        let responseDigdir = postSO('dialogs', dialog);
        expectStatusFor(responseDigdir).to.equal(201);

        expect(responseDigdir, 'response').to.have.validJsonBody();
        expect(responseDigdir.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

        dialogs.push({
            id: responseDigdir.json(),
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

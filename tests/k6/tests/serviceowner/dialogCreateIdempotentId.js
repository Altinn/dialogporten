import {describe, expect, expectStatusFor, postSO, purgeSO} from '../../common/testimports.js'
import {default as dialogToInsert} from './testdata/01-create-dialog.js';


export default function () {

    const dialogs = [];

    const navOrg = {
        orgName: "nav",
        orgNo: "889640782",
        scopes: "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search digdir:dialogporten.serviceprovider.legacyhtml altinn:system/notifications.condition.check digdir:dialogporten.correspondence"
    };

    describe('Attempt to create dialog with unused idempotentId', () => {
        let dialog = dialogToInsert();
        let rnd = Math.floor(Math.random() * 1000);
        let idempotentId = "idempotent" + rnd;

        dialog.idempotentId = idempotentId;
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);
        dialogs.push({
            id: r.json(),
            org: null
        });
    })

    // Amund: Trenger å bytte ut Org for å kunne teste
    describe('Attempt to create dialog with same idempotentId different Org', () => {
        let rnd = Math.floor(Math.random() * 1000);
        let idempotentId = "idempotent" + rnd;
        let dialog = dialogToInsert();
        dialog.idempotentId = idempotentId;
        dialog.serviceResource = "urn:altinn:resource:app_nav_barnehagelister";
        dialog.activities[2].performedBy.actorId = "urn:altinn:organization:identifier-no:889640782";

        let responseNav = postSO('dialogs', dialog, null, navOrg);
        expectStatusFor(responseNav).to.equal(201);
        expect(responseNav, 'response').to.have.validJsonBody();
        expect(responseNav.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

        dialogs.push({
            id: responseNav.json(),
            org: navOrg
        })

        dialog = dialogToInsert();
        dialog.idempotentId = idempotentId;

        let responseDigdir = postSO('dialogs', dialog);
        expectStatusFor(responseDigdir).to.equal(201);
        expect(responseDigdir, 'response').to.have.validJsonBody();
        expect(responseDigdir.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

        dialogs.push({
            id: responseDigdir.json(),
            org: null
        });
    })

    describe('Attempt to create dialog with used idempotentId', () => {
        let dialog = dialogToInsert();
        let rnd = Math.floor(Math.random() * 1000);
        let idempotentId = "idempotent" + rnd;

        dialog.idempotentId = idempotentId;
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);
       
        dialogs.push({
            id: r.json(),
            org: null
        });

        r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(409);
        expect(r, 'response').to.have.validJsonBody();
    })

    describe('Cleanup', () => {
        let i;
        for (i = 0; i < dialogs.length; i++) {
            console.log('Purging dialog ' + dialogs[i])
            let r = purgeSO('dialogs/' + dialogs[i].id, null, dialogs[i].org);
            expectStatusFor(r).to.equal(204);
        }
        expect(dialogs.length).to.equal(i);
    });
}

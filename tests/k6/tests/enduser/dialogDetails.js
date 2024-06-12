import {
    describe, expect, expectStatusFor,
    getEU,
    setVisibleFrom,
    postSO,
    purgeSO } from '../../common/testimports.js'

import { default as dialogToInsert } from '../serviceowner/testdata/01-create-dialog.js';
export default function () {

    let dialogId = null;
    let dialog = null;

    const availableExternalResource = "urn:altinn:resource:app_digdir_be-om-api-nokkel"; // should have "read" on this
    const unavailableExternalResource = "urn:altinn:resource:ttd-altinn-events-automated-tests"; // should not have "read" on this
    const unavailableSubresource = "someunavailablesubresource"; // should not have "elementread" on this;

    describe('Arrange: Create a dialog to test against', () => {
        let d = dialogToInsert();
        d.elements[0].authorizationAttribute = availableExternalResource;
        d.elements[1].authorizationAttribute = unavailableExternalResource;
        d.elements[2].authorizationAttribute = unavailableSubresource;
        setVisibleFrom(d, null);
        let r = postSO("dialogs", d);
        expectStatusFor(r).to.equal(201);
        dialogId = r.json();
    });

    describe('Perform simple dialog get', () => {
        let r = getEU('dialogs/' + dialogId);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();

        dialog = r.json();

        expect(dialog.id, 'dialog id').to.equal(dialogId);
    });

    describe('Check that seen log has been populated', () => {
        if (dialog == null) return;
        expect(dialog.seenSinceLastUpdate, 'seenSinceLastUpdate').to.have.lengthOf(1);
        expect(dialog.seenSinceLastUpdate[0].endUserIdHash, 'endUserIdHash').to.have.lengthOf(10);
        expect(dialog.seenSinceLastUpdate[0].isCurrentEndUser, 'isCurrentEndUser').to.equal(true);
    });

    describe('Check that authorized actions have real URLs', () => {
        if (dialog == null) return;
        expect(dialog, 'dialog').to.have.property("guiActions").with.lengthOf(2);
        expect(dialog.guiActions[0], 'first gui action').to.have.property("isAuthorized").to.equal(true);
        expect(dialog.guiActions[0], 'first gui action').to.have.property("url");
        expect(dialog.guiActions[0].url, 'first gui action url').to.include("https://");
        expect(dialog.guiActions[1], 'second gui action').to.have.property("prompt");
        expect(dialog.guiActions[1].httpMethod, 'second gui action httpMethod').to.equal("POST");
    });

    describe('Check that unauthorized actions to have default URLs', () => {
        if (dialog == null) return;
        expect(dialog, 'dialog').to.have.property("apiActions").with.lengthOf(1);
        expect(dialog.apiActions[0], 'api action').to.have.property("isAuthorized").to.equal(false);
        expect(dialog.apiActions[0], 'url').to.have.property("endpoints");
        for (let i=0; i<dialog.apiActions[0].length; i++) {
            console.log(dialog.apiActions[i]);
            expect(dialog.apiActions[i], 'endpoint').to.have.property("url").to.equal("urn:dialogporten:unauthorized");
        }
    });

    describe('Check that we are authorized for the dialog element referring an external resource', () => {
        if (dialog == null) return;
        expect(dialog, 'dialog').to.have.property("elements");
        expect(dialog.elements.find(x => x.authorizationAttribute == unavailableExternalResource), 'element with unavaiable external resource').to.have.property("isAuthorized").to.equal(false);
        expect(dialog.elements.find(x => x.authorizationAttribute == unavailableSubresource), 'element with unavaiable subresource').to.have.property("isAuthorized").to.equal(false);
        expect(dialog.elements.find(x => x.authorizationAttribute == availableExternalResource), 'element with avaiable external resource').to.have.property("isAuthorized").to.equal(true);
    });

    describe("Cleanup", () => {
        if (dialog == null) return;
        let r = purgeSO("dialogs/" + dialogId);
        expect(r.status, 'response status').to.equal(204);
    });

    describe("Check if we get 404 Not found", () => {
        if (dialog == null) return;
        let r = getEU('dialogs/' + dialogId);
        expectStatusFor(r).to.equal(404);
    });
}

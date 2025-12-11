import {
    describe,
    expect,
    expectStatusFor,
    getSO,
    postSO,
    purgeSO,
    uuidv4,
    setTitle,
    setIsApiOnly,
    setServiceOwnerLabels,
    customConsole as console
} from '../../common/testimports.js'

import { default as dialogToInsert } from './testdata/01-create-dialog.js';

export default function () {
    let dialogIds = [];

    var tag = "so-api-only-dialog-test-" + uuidv4().substring(0, 8);
    var defaultFilter = '?ServiceOwnerLabels=' + tag;

    describe('Create API-only dialog without content', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, true);
        setServiceOwnerLabels(dialog, [tag]);

        // Remove content test validation rule relaxation for API-only dialogs
        delete dialog.content;
        dialog.transmissions.forEach(t => delete t.content);

        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();

        dialogIds.push(r.json());
    });

    describe('Create API-only dialog with content', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, true);
        setServiceOwnerLabels(dialog, [tag]);

        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();

        dialogIds.push(r.json());
    });

    describe('Create API-only dialog with only transmission content', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, true);
        setServiceOwnerLabels(dialog, [tag]);

        delete dialog.content;

        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();

        dialogIds.push(r.json());
    });

    describe('Create API-only dialog with only dialog content', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, true);
        setServiceOwnerLabels(dialog, [tag]);

        dialog.transmissions.forEach(t => delete t.content);

        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();

        dialogIds.push(r.json());
    });

    describe('Create regular dialog with IsApiOnly=false', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, false);
        setServiceOwnerLabels(dialog, [tag]);

        let uniqueTitle = "Regular dialog test " + uuidv4().substring(0, 8);
        setTitle(dialog, uniqueTitle);

        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();

        dialogIds.push(r.json());
    });

    describe('Search for dialogs including API-only dialogs', () => {
        let r = getSO('dialogs' + defaultFilter);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json().items).to.not.be.undefined;

        expect(dialogIds.length).to.equal(r.json().items.length);
    });

    describe('Search for dialogs excluding API-only dialogs', () => {
        let r = getSO('dialogs' + defaultFilter + '&ExcludeApiOnly=true');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json().items).to.not.be.undefined;

        for (let i = 0; i < r.json().items.length; i++) {
            expect(r.json().items[i].isApiOnly).to.be.false;
        }
    });

    describe('Create regular dialog with empty title should fail', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, false);

        // Remove title should fail for regular dialogs
        delete dialog.content.Title;

        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(400);  // Bad Request due to validation failure
        expect(r, 'response').to.have.validJsonBody();

        // Just test that there's an error response with validation details, without specifying the exact path
        expect(r.json(), 'error details').to.be.an('object').and.not.to.be.empty;
    });

    describe('Create API-only dialog with empty (non-null) content should fail', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, true);

        // If content is supplied at all, it must pass regular validation
        delete dialog.content.Title;

        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(400);  // Bad Request due to validation failure
        expect(r, 'response').to.have.validJsonBody();

        // Just test that there's an error response with validation details, without specifying the exact path
        expect(r.json(), 'error details').to.be.an('object').and.not.to.be.empty;
    });

    describe('Create API-only dialog with empty (non-null) transmission content should fail', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, true);

        // If content is supplied at all, it must pass regular validation
        delete dialog.content;
        dialog.transmissions.forEach(t => delete t.content);
        dialog.transmissions[0].content = {}; // Empty object instead of null

        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(400);  // Bad Request due to validation failure
        expect(r, 'response').to.have.validJsonBody();

        // Just test that there's an error response with validation details, without specifying the exact path
        expect(r.json(), 'error details').to.be.an('object').and.not.to.be.empty;
    });

    describe('Cleanup', () => {
        for (let id of dialogIds) {
            if (id) {
                let r = purgeSO('dialogs/' + id);
                expectStatusFor(r).to.equal(204);
            }
        }
    });
}

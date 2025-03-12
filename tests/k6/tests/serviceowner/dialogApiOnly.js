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
    customConsole as console
} from '../../common/testimports.js'

import { default as dialogToInsert } from './testdata/01-create-dialog.js';

export default function () {
    let dialogIds = [];

    describe('Create API-only dialog with empty title and summary', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, true);
        
        // Remove title and summary to test validation rule relaxation for API-only dialogs
        delete dialog.content.Title;
        delete dialog.content.Summary;
        
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        
        dialogIds.push(r.json());
    });

    describe('Search for dialogs including API-only dialogs', () => {
        let r = getSO('dialogs?CreatedAfter=2023-01-01');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        
        // Verify that our API-only dialog is included in the results when IncludeApiOnly=true (default)
        let apiOnlyDialogId = dialogIds[0];
        let apiOnlyDialogFound = false;
        
        for (let i = 0; i < r.json().items.length; i++) {
            if (r.json().items[i].id === apiOnlyDialogId) {
                apiOnlyDialogFound = true;
                break;
            }
        }
        
        expect(apiOnlyDialogFound, 'API-only dialog found').to.be.true;
    });

    describe('Search for dialogs excluding API-only dialogs', () => {
        let r = getSO('dialogs?IncludeApiOnly=false&CreatedAfter=2023-01-01');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        
        // Verify that our API-only dialog is excluded when IncludeApiOnly=false
        let apiOnlyDialogId = dialogIds[0];
        let apiOnlyDialogFound = false;
        
        for (let i = 0; i < r.json().items.length; i++) {
            if (r.json().items[i].id === apiOnlyDialogId) {
                apiOnlyDialogFound = true;
                break;
            }
        }
        
        expect(apiOnlyDialogFound, 'API-only dialog not found').to.be.false;
    });

    describe('Create regular dialog with IsApiOnly=false', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, false);
        
        let uniqueTitle = "Regular dialog test " + uuidv4().substring(0, 8);
        setTitle(dialog, uniqueTitle);
        
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        
        dialogIds.push(r.json());
    });

    describe('Create regular dialog with empty title should fail', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, false);

        // Remove title should fail for regular dialogs
        delete dialog.content.Title;
        
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(400);  // Bad Request due to validation failure
        expect(r, 'response').to.have.validJsonBody();
        
        // Examine the error response in more detail
        console.log("Error response: " + JSON.stringify(r.json()));
        
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
import {
    describe,
    expect,
    expectStatusFor,
    getSO,
    postSO,
    putSO,
    purgeSO,
    uuidv4,
    setTitle,
    setIsApiOnly,
    customConsole as console
} from '../../common/testimports.js'

import { default as dialogToInsert } from './testdata/01-create-dialog.js';

export default function () {
    let dialogIds = [];

    describe('Create a regular dialog for update testing', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, false); // Explicitly set to false for clarity
        
        let uniqueTitle = "Update test dialog " + uuidv4().substring(0, 8);
        setTitle(dialog, uniqueTitle);
        
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        
        dialogIds.push(r.json());
    });
    
    describe('Update regular dialog to API-only with empty title', () => {
        // First get the dialog to update
        let dialogId = dialogIds[0];
        let r = getSO(`dialogs/${dialogId}`);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        
        let dialog = r.json();
        
        // Set future dates for ExpiresAt and VisibleFrom
        const futureDate = new Date();
        futureDate.setDate(futureDate.getDate() + 30); // 30 days in the future
        const futureDateISOString = futureDate.toISOString();
        
        // Create a simplified version of the dialog for update
        let updateDialog = {
            isApiOnly: true,
            content: {
                // Keeping other content values but removing Title and Summary
                SenderName: dialog.content.SenderName,
                AdditionalInfo: dialog.content.AdditionalInfo,
                ExtendedStatus: dialog.content.ExtendedStatus
            },
            status: dialog.status,
            visibleFrom: futureDateISOString,
            expiresAt: futureDateISOString
        };
        
        // Update the dialog
        let updateResponse = putSO(`dialogs/${dialogId}`, updateDialog, { 
            headers: { "If-Match": dialog.revision } 
        });
        expectStatusFor(updateResponse).to.equal(204); // No Content response
        // No need to check for JSON body since 204 responses have no content
        
        // Verify the update worked
        let verifyResponse = getSO(`dialogs/${dialogId}`);
        expectStatusFor(verifyResponse).to.equal(200);
        expect(verifyResponse, 'verify response').to.have.validJsonBody();
    });
    
    describe('Create an API-only dialog for update testing', () => {
        let dialog = dialogToInsert();
        setIsApiOnly(dialog, true);
        
        // Remove title and summary since it's API-only
        delete dialog.content.Title;
        delete dialog.content.Summary;
        
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        
        dialogIds.push(r.json());
    });
    
    describe('Update API-only dialog to regular dialog should require title', () => {
        // First get the dialog to update
        let dialogId = dialogIds[1];
        let r = getSO(`dialogs/${dialogId}`);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        
        let dialog = r.json();
        
        // Set future dates for ExpiresAt and VisibleFrom
        const futureDate = new Date();
        futureDate.setDate(futureDate.getDate() + 30); // 30 days in the future
        const futureDateISOString = futureDate.toISOString();
        
        // Create a simplified version of the dialog for update
        let updateDialog = {
            isApiOnly: false,
            content: {
                // Keeping other content values without adding Title/Summary
                SenderName: dialog.content.SenderName,
                AdditionalInfo: dialog.content.AdditionalInfo,
                ExtendedStatus: dialog.content.ExtendedStatus
            },
            status: dialog.status,
            visibleFrom: futureDateISOString,
            expiresAt: futureDateISOString
        };
        
        // Update should fail since it now requires title and summary
        let updateResponse = putSO(`dialogs/${dialogId}`, updateDialog, { 
            headers: { "If-Match": dialog.revision } 
        });
        expectStatusFor(updateResponse).to.equal(400); // Bad Request due to validation
        expect(updateResponse, 'error response').to.have.validJsonBody();
        
        // Check error message contains information about the missing title for non-API-only dialogs
        console.log("Error response: " + JSON.stringify(updateResponse.json()));
        expect(updateResponse.json().errors["dto.content.Title"], 'error contains Title validation')
            .to.not.be.undefined;
    });
    
    describe('Update API-only dialog to regular dialog with title should succeed', () => {
        // First get the dialog to update
        let dialogId = dialogIds[1];
        let r = getSO(`dialogs/${dialogId}`);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        
        let dialog = r.json();
        
        // Set future dates for ExpiresAt and VisibleFrom
        const futureDate = new Date();
        futureDate.setDate(futureDate.getDate() + 30); // 30 days in the future
        const futureDateISOString = futureDate.toISOString();
        
        // Create a simplified version of the dialog for update
        let updateDialog = {
            isApiOnly: false,
            content: {
                Title: {
                    value: [{ languageCode: "nb", value: "Updated title" }]
                },
                Summary: {
                    value: [{ languageCode: "nb", value: "Updated summary" }]
                },
                SenderName: dialog.content.SenderName,
                AdditionalInfo: dialog.content.AdditionalInfo,
                ExtendedStatus: dialog.content.ExtendedStatus
            },
            status: dialog.status,
            visibleFrom: futureDateISOString,
            expiresAt: futureDateISOString
        };
        
        // Update should succeed
        let updateResponse = putSO(`dialogs/${dialogId}`, updateDialog, { 
            headers: { "If-Match": dialog.revision } 
        });
        expectStatusFor(updateResponse).to.equal(204); // No Content response
        // No need to check for JSON body since 204 responses have no content
        
        // Verify the update worked
        let verifyResponse = getSO(`dialogs/${dialogId}`);
        expectStatusFor(verifyResponse).to.equal(200);
        expect(verifyResponse, 'verify response').to.have.validJsonBody();
        
        // Verify Title was added correctly - validate we can see the updated content
        // This indirectly proves the API-only flag was set to false, since we can see the title
        const responseJson = verifyResponse.json();
        const content = responseJson.content || responseJson.Content;
        
        // Just verify that we have some content with a Title value we can access
        expect(content, 'content object').to.not.be.undefined;
        expect(content.Title || content.title, 'title property').to.not.be.undefined;
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
import {
    describe,
    expect,
    expectStatusFor,
    getSO,
    postSO,
    purgeSO,
    deleteSO,
    customConsole as console
} from '../../common/testimports.js'

import { default as dialogToInsert } from './testdata/01-create-dialog.js';

export default function () {
    describe('Cannot delete label that does not exist', () => {
        let dialogToCreate = dialogToInsert();

        // Create the dialog with labels
        let createResponse = postSO('dialogs', dialogToCreate);
        expectStatusFor(createResponse).to.equal(201);

        let dialogId = createResponse.json();

        // Attempt to delete a non-existent label
        let nonExistentLabel = "nonExistentLabel";
        let deleteResponse = deleteSO('dialogs/' + dialogId + '/context/labels/' + nonExistentLabel);
        expectStatusFor(deleteResponse).to.equal(404);

        // Verify error message in the response
        let responseString = JSON.stringify(deleteResponse.json());
        expect(responseString).to.contain('not found');
        expect(responseString).to.contain(nonExistentLabel);

        // Cleanup
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);
    });

    describe('Can delete label', () => {
        let dialogToCreate = dialogToInsert();
        let labels = [
            { value: 'label1' },
            { value: 'label2' }
        ];
        dialogToCreate.serviceOwnerContext.ServiceOwnerLabels = labels;

        // Create the dialog with labels
        let createResponse = postSO('dialogs', dialogToCreate);
        expectStatusFor(createResponse).to.equal(201);

        let dialogId = createResponse.json();

        // Delete a label
        let labelToDelete = labels[0].value;
        let deleteResponse = deleteSO('dialogs/' + dialogId + '/context/labels/' + labelToDelete);
        expectStatusFor(deleteResponse).to.equal(204);

        // Verify label is deleted
        let getResponse = getSO('dialogs/' + dialogId + '/context/labels');
        expectStatusFor(getResponse).to.equal(200);
        let responseBody = getResponse.json();
        expect(responseBody).to.have.length(labels.length - 1);
        expect(responseBody.some(label => label.value === labelToDelete)).to.be.false;

        // Cleanup
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);
    });
}

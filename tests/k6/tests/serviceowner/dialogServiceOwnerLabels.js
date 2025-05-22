import {
    describe,
    expect,
    expectStatusFor,
    getSO,
    postSO,
    purgeSO,
    deleteSO,
    uuidv4,
    setTitle,
    setIsApiOnly,
    customConsole as console
} from '../../common/testimports.js'

import { default as dialogToInsert } from './testdata/01-create-dialog.js';

export default function () {

    describe('Can create a dialog with labels', () => {
        let dialogToCreate = dialogToInsert();
        let label = "some-label";
        dialogToCreate.serviceOwnerContext.ServiceOwnerLabels = [{value: label}]

        let createResponse = postSO('dialogs', dialogToCreate);

        let dialogId = createResponse.json();

        let getResponse = getSO('dialogs/' + dialogId);
        expectStatusFor(getResponse).to.equal(200);
        expect(getResponse, 'response').to.have.validJsonBody();
        let dialog = getResponse.json();
        expect(dialog).to.have.property('serviceOwnerContext');
        expect(dialog.serviceOwnerContext.serviceOwnerLabels[0].value).to.equal(label);

        // Cleanup
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);
    });

    describe('Cannot create a dialog with duplicate labels', () => {
        let dialogToCreate = dialogToInsert();
        let label = "LABEL";
        dialogToCreate.serviceOwnerContext.ServiceOwnerLabels = [
            { value: label },
            { value: label.toLowerCase() }
        ];

        let createResponse = postSO('dialogs', dialogToCreate);
        expectStatusFor(createResponse).to.equal(400);
        expect(createResponse, 'response').to.have.validJsonBody();

        let responseString = JSON.stringify(createResponse.json());
        expect(responseString).to.contain('duplicate');
    });

    describe('Cannot create a dialog with invalid length labels', () => {
        let dialogToCreate = dialogToInsert();
        let labels = [
            {value: undefined},
            {value: 'a'},
            {value: new Array(300).fill('a').join('')}
        ]
        dialogToCreate.serviceOwnerContext.ServiceOwnerLabels = labels;

        let createResponse = postSO('dialogs', dialogToCreate);
        expectStatusFor(createResponse).to.equal(400);

        let responseString = JSON.stringify(createResponse.json());
        expect(responseString).to.contain('not be empty');
        expect(responseString).to.contain('at least');
        expect(responseString).to.contain('or fewer');
    });

    describe('Cannot create a dialog with more than maximum allowed labels', () => {
        let dialogToCreate = dialogToInsert();
        let maximumLabels = 20;

        let labels = [];
        for (let i = 0; i < maximumLabels + 1; i++) {
            labels.push({ value: `label${i}` });
        }
        dialogToCreate.serviceOwnerContext.ServiceOwnerLabels = labels;

        let createResponse = postSO('dialogs', dialogToCreate);
        expectStatusFor(createResponse).to.equal(400);

        let responseString = JSON.stringify(createResponse.json());
        expect(responseString).to.contain('Maximum');
        expect(responseString).to.contain(`${maximumLabels}`);
    });

    describe('Can retrieve all labels and service owner context revision', () => {
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

        // Retrieve all labels
        let getResponse = getSO('dialogs/' + dialogId + '/context/labels');
        expectStatusFor(getResponse).to.equal(200);
        expect(getResponse, 'response').to.have.validJsonBody();

        // Verify labels in the response
        let responseBody = getResponse.json();
        expect(responseBody).to.have.length(labels.length);

        // Verify ETag header is present
        let eTagHeader = getResponse.headers['Etag'];
        expect(eTagHeader, 'ETag header').to.not.be.undefined;

        // Cleanup
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);
    });


    describe('Can create labels via create endpoint', () => {
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

        // Create labels via dedicated endpoint
        let newLabel = { value: "newLabel" };
        let labelCreationResponse = postSO('dialogs/' + dialogId + '/context/labels', newLabel);
        expectStatusFor(labelCreationResponse).to.equal(204);

        // Verify labels in the response
        let getResponse = getSO('dialogs/' + dialogId + '/context/labels');
        expectStatusFor(getResponse).to.equal(200);
        expect(getResponse, 'response').to.have.validJsonBody();
        let responseBody = getResponse.json();
        expect(responseBody).to.have.length(3);

        // Cleanup
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);
    });

    describe('Cannot create duplicate labels via create endpoint', () => {
        let dialogToCreate = dialogToInsert();
        let someLabel = { value: "some-label" };
        let labels = [someLabel];
        dialogToCreate.serviceOwnerContext.ServiceOwnerLabels = labels;

        // Create the dialog with labels
        let createResponse = postSO('dialogs', dialogToCreate);
        expectStatusFor(createResponse).to.equal(201);

        let dialogId = createResponse.json();

        // Attempt to create duplicate labels again
        let duplicateLabelCreationResponse = postSO('dialogs/' + dialogId + '/context/labels', someLabel);
        expectStatusFor(duplicateLabelCreationResponse).to.equal(400);

        // Verify error message in the response
        let responseString = JSON.stringify(duplicateLabelCreationResponse.json());
        expect(responseString).to.contain('duplicate');

        // Cleanup
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);
    });

    describe('Cannot create label with invalid value via create endpoint', () => {
        let dialogToCreate = dialogToInsert();

        // Create the dialog with labels
        let createResponse = postSO('dialogs', dialogToCreate);
        expectStatusFor(createResponse).to.equal(201);

        let dialogId = createResponse.json();

        let undefinedLabel = { value: undefined };
        let undefinedLabelCreationResponse = postSO('dialogs/' + dialogId + '/context/labels', undefinedLabel);
        expectStatusFor(undefinedLabelCreationResponse).to.equal(400);

        // Verify error message in the response
        let responseString = JSON.stringify(undefinedLabelCreationResponse.json());
        expect(responseString).to.contain('not be empty');

        let longLabel = { value: new Array(300).fill('a').join('') };
        let longLabelCreationResponse = postSO('dialogs/' + dialogId + '/context/labels', longLabel);
        expectStatusFor(longLabelCreationResponse).to.equal(400);

        // Verify error message in the response
        responseString = JSON.stringify(longLabelCreationResponse.json());
        expect(responseString).to.contain('or fewer');

        let shortLabel = { value: 'a' };
        let shortLabelCreationResponse = postSO('dialogs/' + dialogId + '/context/labels', shortLabel);
        expectStatusFor(shortLabelCreationResponse).to.equal(400);

        // Verify error message in the response
        responseString = JSON.stringify(shortLabelCreationResponse.json());
        expect(responseString).to.contain('at least');

        // Cleanup
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);
    });

    describe('Adding new label returns new revision header', () => {
        let dialogToCreate = dialogToInsert();

        // Create the dialog with labels
        let createResponse = postSO('dialogs', dialogToCreate);
        expectStatusFor(createResponse).to.equal(201);

        let dialogId = createResponse.json();

        // Fetch the labels
        let getResponse = getSO('dialogs/' + dialogId + '/context/labels');
        expectStatusFor(getResponse).to.equal(200);
        let initialRevision = getResponse.headers['Etag'];

        // Add a new label
        let newLabel = { value: "newLabel" };
        let labelCreationResponse = postSO('dialogs/' + dialogId + '/context/labels', newLabel);
        expectStatusFor(labelCreationResponse).to.equal(204);

        // Verify header has changed
        let newRevision = labelCreationResponse.headers['Etag'];
        expect(newRevision).to.not.equal(initialRevision);

        // Cleanup
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);
    });

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

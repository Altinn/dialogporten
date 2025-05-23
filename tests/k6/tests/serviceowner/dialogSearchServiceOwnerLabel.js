import { describe, expect, expectStatusFor, getSO, postSO, purgeSO } from '../../common/testimports.js';

import { default as dialogToInsert } from './testdata/01-create-dialog.js';

export default function () {
    describe('Can filter on single label', () => {
        // Arrange
        let createDialogWithoutLabel = dialogToInsert();
        let createResponse1 = postSO('dialogs', createDialogWithoutLabel);
        expectStatusFor(createResponse1).to.equal(201);
        let dialogId1 = createResponse1.json();

        const label = "Scadrial";
        let labeledDialog = dialogToInsert();
        labeledDialog.serviceOwnerContext.serviceOwnerLabels = [{ value: label }];
        let createResponse2 = postSO('dialogs', labeledDialog);
        expectStatusFor(createResponse2).to.equal(201);

        let dialogId2 = createResponse2.json();

        // Act
        let searchResponse = getSO(`dialogs?serviceOwnerLabels=${label}`);

        // Assert
        expectStatusFor(searchResponse).to.equal(200);
        let result = searchResponse.json();
        expect(result.items).to.have.length(1);
        expect(result.items[0].id).to.equal(dialogId2);

        // Cleanup
        let purgeResponse1 = purgeSO('dialogs/' + dialogId2);
        expectStatusFor(purgeResponse1).to.equal(204);

        let purgeResponse2 = purgeSO('dialogs/' + dialogId1);
        expectStatusFor(purgeResponse2).to.equal(204);
    });

    describe('Multiple label inputs must all match', () => {
        // Arrange
        const label1 = "Scadrial";
        const label2 = "Roshar";

        let dialogWithOneLabel = dialogToInsert();
        dialogWithOneLabel.serviceOwnerContext.serviceOwnerLabels = [{ value: label1 }];
        let createResponse1 = postSO('dialogs', dialogWithOneLabel);
        let dialogId1 = createResponse1.json();

        let dialogWithTwoLabels = dialogToInsert();
        dialogWithTwoLabels.serviceOwnerContext.serviceOwnerLabels = [{ value: label1 }, { value: label2 }];
        let createResponse = postSO('dialogs', dialogWithTwoLabels);
        let dialogId = createResponse.json();

        // Act
        let searchResponse = getSO(`dialogs?serviceOwnerLabels=${label1}&serviceOwnerLabels=${label2}`);

        // Assert
        expectStatusFor(searchResponse).to.equal(200);
        let result = searchResponse.json();

        expect(result.items).to.have.length(1);
        expect(result.items[0].id).to.equal(dialogId);

        // Cleanup
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);

        let purgeResponse1 = purgeSO('dialogs/' + dialogId1);
        expectStatusFor(purgeResponse1).to.equal(204);
    });

    describe('Can filter on label prefix', () => {
        // Arrange
        const label1 = "ScadrialOne";
        const label2 = "ScadrialTwo";

        let dialogWithLabel1 = dialogToInsert();
        dialogWithLabel1.serviceOwnerContext.serviceOwnerLabels = [{ value: label1 }];
        var dialogWithLabel1Result = postSO('dialogs', dialogWithLabel1);
        let dialogId1 = dialogWithLabel1Result.json();

        let dialogWithLabel2 = dialogToInsert();
        dialogWithLabel2.serviceOwnerContext.serviceOwnerLabels = [{ value: label2 }];
        let dialogWithLabel2Result = postSO('dialogs', dialogWithLabel2);
        let dialogId2 = dialogWithLabel2Result.json();

        // Act
        let searchResponse = getSO(`dialogs?serviceOwnerLabels=scad*`);

        // Assert
        expectStatusFor(searchResponse).to.equal(200);
        let result = searchResponse.json();
        expect(result.items).to.have.length(2);

        // Cleanup
        let purgeResponse1 = purgeSO('dialogs/' + dialogId1);
        expectStatusFor(purgeResponse1).to.equal(204);
        let purgeResponse2 = purgeSO('dialogs/' + dialogId2);
        expectStatusFor(purgeResponse2).to.equal(204);
    });

    describe('Filtering on non-existing label returns no results', () => {
        // Arrange
        let createDialog = dialogToInsert();
        let createResponse = postSO('dialogs', createDialog);
        expectStatusFor(createResponse).to.equal(201);

        // Act
        let searchResponse = getSO(`dialogs?serviceOwnerLabels=NonExistingLabel`);

        // Assert
        expectStatusFor(searchResponse).to.equal(200);
        let result = searchResponse.json();
        expect(result.items).to.be.undefined;

        // Cleanup
        let dialogId = createResponse.json();
        let purgeResponse = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeResponse).to.equal(204);
    });

    describe('Cannot filter on invalid label length', () => {
        // Act
        let searchResponse = getSO(`dialogs?serviceOwnerLabels=a&serviceOwnerLabels=${'a'.repeat(300)}`);

        // Assert
        expectStatusFor(searchResponse).to.equal(400);
        let responseString = JSON.stringify(searchResponse.json());
        console.log(responseString);
        expect(responseString).to.contain('at least');
        expect(responseString).to.contain('or fewer');
    });
}

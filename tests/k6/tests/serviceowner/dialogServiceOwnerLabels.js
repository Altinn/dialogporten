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
    })

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
            {value: 'a'},
            {value: new Array(300).fill('a').join('')}
        ]
        dialogToCreate.serviceOwnerContext.ServiceOwnerLabels = labels;

        let createResponse = postSO('dialogs', dialogToCreate);
        expectStatusFor(createResponse).to.equal(400);

        let responseString = JSON.stringify(createResponse.json());
        expect(responseString).to.contain('at least');
        expect(responseString).to.contain('or fewer');
    })

    describe('Cannot create a dialog with more than maximum labels', () => {
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
    })
}

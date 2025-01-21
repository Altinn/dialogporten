import { describe, expect, expectStatusFor, postSO } from '../../common/testimports.js'
import { default as dialogToInsert } from './testdata/01-create-dialog.js';

export default function () {

    let expectGuiActionErrorResponseForDialog = function(dialog) {
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(400);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'reponse').to.have.property('errors');
        expect(r.json().errors, 'error').to.have.property('dto.guiActions');
    };

    let createDialogWithGuiActions = function(numActions, actionType) {
        let dialog = dialogToInsert();
        dialog.guiActions = [];
        while (numActions--) {
            let id = `foo${numActions}`;
            dialog.guiActions.push(
                { action: id, title: [{ languageCode: "nb", value: id }], url: `foo:${id}`, priority: actionType }
            )
        }

        return dialog;
    };

    describe('Attempt dialog create with two primary actions', () => {
        expectGuiActionErrorResponseForDialog(createDialogWithGuiActions(2, "primary"));
    });

    describe('Attempt dialog create with two secondary actions', () => {
        expectGuiActionErrorResponseForDialog(createDialogWithGuiActions(2, "secondary"));
    });

    describe('Attempt dialog create with six tertiary actions', () => {
        expectGuiActionErrorResponseForDialog(createDialogWithGuiActions(6, "tertiary"));
    });
}

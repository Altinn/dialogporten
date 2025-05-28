import { describe, expect, expectStatusFor, postSO, postEU, getEU, purgeSO, setParty, setVisibleFrom } from '../../common/testimports.js'
import { default as dialogToInsert } from '../serviceowner/testdata/01-create-dialog.js'
import { defaultServiceOwnerOrgNo } from '../../common/config.js'

export default function () {
    const accessibleDialogs = [];
    let forbiddenDialog = null;

    describe('Create dialogs to bulk update', () => {
        for (let i = 0; i < 2; i++) {
            let d = dialogToInsert();
            setVisibleFrom(d, null);
            const r = postSO('dialogs', d, null);
            expectStatusFor(r).to.equal(201);
            accessibleDialogs.push(r.json());
        }
        const d = dialogToInsert();
        setParty(d, 'urn:altinn:organization:identifier-no:' + defaultServiceOwnerOrgNo);
        setVisibleFrom(d, null);
        const r = postSO('dialogs', d);
        expectStatusFor(r).to.equal(201);
        forbiddenDialog = r.json();
    });

    describe('Bulk set labels for accessible dialogs', () => {
        const body = { dialogs: accessibleDialogs.map(id => ({ dialogId: id })), systemLabels: ['Bin'] };
        const r = postEU('dialogs/context/systemlabels/actions/bulkset', body);
        expectStatusFor(r).to.equal(204);
        accessibleDialogs.forEach(id => {
            const r2 = getEU('dialogs/' + id);
            expectStatusFor(r2).to.equal(200);
            expect(r2.json()['systemLabel'], 'system label').to.equal('Bin');
        });
    });

    describe('Bulk set containing unauthorized dialog returns 403', () => {
        const body = {
            dialogs: accessibleDialogs.concat([forbiddenDialog]).map(id => ({ dialogId: id })),
            systemLabels: ['Archive']
        };
        const r = postEU('dialogs/context/systemlabels/actions/bulkset', body);
        expectStatusFor(r).to.equal(403);
    });

    describe('Cleanup', () => {
        accessibleDialogs.concat([forbiddenDialog]).forEach(id => {
            const r = purgeSO('dialogs/' + id);
            expectStatusFor(r).to.equal(204);
        });
    });
}

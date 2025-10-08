import {
    describe, expect, expectStatusFor,
    getEU,
    getSysEU,
    setVisibleFrom,
    postSO,
    purgeSO } from '../../common/testimports.js'

import { default as dialogToInsert } from '../serviceowner/testdata/01-create-dialog.js';
export  default function (){
    describe('SystemUser with empty party list', () => {
        // Arrange
        let dialog = dialogToInsert();
        setVisibleFrom(dialog, null);

        let r = postSO("dialogs", dialog);
        expectStatusFor(r).to.equal(201);
        let dialogId = r.json();
        
        
        // Assert
        // let getRes = getEU('dialogs/' + dialogId);
        // expectStatusFor(getRes).to.equal(200);


        let getResSys = getSysEU('dialogs/' + dialogId);
        expectStatusFor(getResSys).to.equal(404);
        // Clean up
        let purgeResponse = purgeSO("dialogs/" + dialogId);
        expect(purgeResponse.status, 'response status').to.equal(204);
    });
}


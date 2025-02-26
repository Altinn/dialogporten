import { describe, expect, expectStatusFor, postSO } from '../../common/testimports.js'
import { default as dialogToInsert } from './testdata/01-create-dialog.js';


export default function (){

    describe ('Attempt to create dialog with invalid URI', () => {
        let dialog = dialogToInsert();
        dialog.process = 'inval|d';
        let r = postSO('dialogs', dialog)
        expectStatusFor(r).to.equal(400);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response body').to.have.property('errors');
    })
}

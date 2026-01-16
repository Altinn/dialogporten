import {
    describe,
    expect,
    expectStatusFor,
    postSO,
    getSO,
    purgeSO,
    setAttachments,
    addAttachment
} from '../../common/testimports.js'
import {default as dialogToInsert} from './testdata/01-create-dialog.js'
import {getDefaultEnduserSsn} from '../../common/token.js'

export default function () {
    describe('dialogAttachmentOrder', () => {
        // Arrange
        let endUserId = "urn:altinn:person:identifier-no:" + getDefaultEnduserSsn();
        let dialog = dialogToInsert();
        setAttachments(dialog, null);
        let attachmentCount = 5;
        for (let i = 0; i < attachmentCount; i++) {
            addAttachment(dialog, {
                    "displayName": [{
                        "languageCode": 'en',
                        "value": `Attachment ${i}`
                    }],
                    "urls": [{
                        "consumerType": "gui",
                        "url": "https://foo.com/foo.pdf",
                        "mediaType": "application/pdf"
                    }]
                }
            );
        }

        let postRes = postSO('dialogs', dialog);
        expectStatusFor(postRes, 'Create dialog').to.equal(201);
        let dialogId = postRes.json();


        // Assert
        let getRes = getSO('dialogs/' + dialogId + '?endUserId=' + endUserId);
        expectStatusFor(getRes, 'Get dialog').to.equal(200);
        expect(getRes).to.have.validJsonBody();
        expect(getRes.json()).to.have.property('attachments').to.have.length(attachmentCount);
        let dialogRes = getRes.json();

        for (let i = 0; i < attachmentCount; i++) {
            let attachment = dialogRes.attachments[i];
            expect(attachment.displayName[0]).to.have.property('value').to.equal(`Attachment ${i}`);
        }

        // Clean up
        let purgeRes = purgeSO('dialogs/' + dialogId);
        expectStatusFor(purgeRes, 'Purge dialog').to.equal(204);
    })
}

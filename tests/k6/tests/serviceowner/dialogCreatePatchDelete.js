import {
    describe,
    expect,
    expectStatusFor,
    getSO,
    postSO,
    purgeSO,
    patchSO,
    uuidv4,
    uuidv7,
    setActivities,
    addActivity,
    setVisibleFrom
} from '../../common/testimports.js'
import {default as dialogToInsert} from './testdata/01-create-dialog.js';

export default function () {

    let dialogIds = [];
    let dialogId = null;
    let attachmentId = null;
    let eTag = null;
    let newApiActionEndpointUrl = null;

    describe('Perform dialog create', () => {
        let d = dialogToInsert();
        setVisibleFrom(d, null);
        let r = postSO('dialogs', d);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/)

        dialogId = r.json();
    });

    describe('Perform dialog create with specifed attachmentId', () => {
        let dialog = dialogToInsert();
        attachmentId = uuidv7();
        dialog.transmissions[0].attachments[0].id = attachmentId;
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/)

        dialogIds.push(r.json());
    });

    describe('Perform dialog create with taken specifed attachmentId', () => {
        let dialog = dialogToInsert();
        dialog.transmissions[0].attachments[0].id = attachmentId;
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(422);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property('errors');

    });
    describe('Perform dialog get', () => {
        let r = getSO('dialogs/' + dialogId);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();

        let rJson = r.json();
        expect(rJson, 'dialog').to.have.property("id").to.equal(dialogId);
        expect(rJson, 'dialog').to.have.property("createdAt");
        expect(rJson.createdAt, 'createdAt').to.match(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}Z$/);
        expect(rJson, 'dialog').to.have.property("updatedAt");
        expect(rJson, 'dialog').to.have.property('revision');

        eTag = r.json()["revision"];
    });

    describe('Deny attempt to patch with invalid If-Match', () => {
        newApiActionEndpointUrl = "https://digdir.no/" + uuidv4();
        let patchDocument = [
            {
                "op": "replace",
                "path": "/apiActions/0/endpoints/1/url",
                "value": newApiActionEndpointUrl
            }
        ];
        let r = patchSO('dialogs/' + dialogId, patchDocument, {"headers": {"If-Match": uuidv4()}});
        expectStatusFor(r).to.equal(412); // Precondition failed
    });

    describe('Allow patch with valid If-Match', () => {
        newApiActionEndpointUrl = "https://digdir.no/" + uuidv4();
        let patchDocument = [
            {
                "op": "replace",
                "path": "/apiActions/0/endpoints/1/url",
                "value": newApiActionEndpointUrl
            }
        ];
        let r = patchSO('dialogs/' + dialogId, patchDocument, {"headers": {"If-Match": eTag}});
        expectStatusFor(r).to.equal(204);
    });

    describe('Perform dialog get after patch', () => {
        let r = getSO('dialogs/' + dialogId);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'dialog').to.have.property("updatedAt");
        expect(r.json().updatedAt, 'updatedAt').to.match(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}Z$/);
        expect(r.json(), 'dialog').to.have.property("createdAt");
        expect(r.json().updatedAt, 'updatedAt').to.not.equal(r.json().createdAt);
        expect(r.json(), 'dialog').to.have.property('revision');
        expect(r.json().revision, 'updated revision').to.not.equal(eTag);
        expect(r.json(), 'dialog').to.have.nested.property("apiActions[0].endpoints[1].url").to.equal(newApiActionEndpointUrl);
    });

    describe('Perform dialog purge', () => {
        let r = purgeSO('dialogs/' + dialogId);
        expectStatusFor(r).to.equal(204);
    });

    describe('Perform dialog create with activity type (dialogOpened)', () => {
        // Setup
        let dialog = dialogToInsert();
        let activities = [{
            'id': uuidv7(),
            'type': 'dialogOpened',
            'performedBy': {
                'actorType': 'ServiceOwner'
            }
        }]

        setActivities(dialog, activities);

        // Act
        let r = postSO('dialogs', dialog);

        // Assert
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/)

        // Cleanup
        r = purgeSO('dialogs/' + r.json());
        expectStatusFor(r).to.equal(204);
    });

    describe('Perform dialog create with invalid activity type (transmissionOpened)', () => {
        // Setup
        let dialog = dialogToInsert();
        let activities = [{
            'id': uuidv7(),
            'type': 'transmissionOpened',
            'performedBy': {
                'actorType': 'ServiceOwner'
            }
        }]

        setActivities(dialog, activities);

        // Act
        let r = postSO('dialogs', dialog);

        // Assert
        expectStatusFor(r).to.equal(400);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property('errors');
    });


    describe('Perform dialog create with activity type (transmissionOpened)', () => {
        // Setup
        let dialog = dialogToInsert();
        let transmissionId = uuidv7();
        dialog.transmissions[0].id = transmissionId;

        let activities = [{
            'id': uuidv7(),
            'type': 'transmissionOpened',
            'transmissionId': transmissionId,
            'performedBy': {
                'actorType': 'ServiceOwner'
            }
        }]

        setActivities(dialog, activities);

        // Act
        let r = postSO('dialogs', dialog);

        // Assert
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/)

        // Cleanup
        r = purgeSO('dialogs/' + r.json());
        expectStatusFor(r).to.equal(204);
    });
    describe('Perform dialog create with invalid activity type (dialogOpened)', () => {
        // Setup
        let dialog = dialogToInsert();
        let transmissionId = uuidv7();
        dialog.transmissions[0].id = transmissionId;

        let activities = [{
            'id': uuidv7(),
            'type': 'DialogOpened',
            'transmissionId': transmissionId,
            'performedBy': {
                'actorType': 'ServiceOwner'
            }
        }]

        setActivities(dialog, activities);

        // Act
        let r = postSO('dialogs', dialog);

        // Assert
        expectStatusFor(r).to.equal(400);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property('errors');
    });

    describe('Remove List properties with Patch', () => {

            let d = dialogToInsert();
            let post = postSO("dialogs", d);
            expectStatusFor(post).to.equal(201);
            expect(post, 'response').to.have.validJsonBody();

            let dialogId = post.json();

            let patchDocument = [
                {
                    "op": "remove",
                    "path": "/guiActions"
                },
                {
                    "op": "remove",
                    "path": "/apiActions"
                },
                {
                    "op": "remove",
                    "path": "/searchTags"
                },
                {
                    "op": "remove",
                    "path": "/Attachments"
                }
            ];

            let patch = patchSO('dialogs/' + dialogId, patchDocument);
            expectStatusFor(patch).to.equal(204);

            let purge = purgeSO('dialogs/' + dialogId);
            expectStatusFor(purge).to.equal(204);
        }
    )

    describe('Clean up', () => {
        for (let i = 0; i < dialogIds.length; i++) {
            let r = purgeSO('dialogs/' + dialogIds[i]);
            expectStatusFor(r).to.equal(204);
        }
    });
}

import {
    describe,
    expect,
    expectStatusFor,
    getSO,
    uuidv4,
    customConsole as console,
    setTitle,
    setAdditionalInfo,
    setSearchTags,
    setSenderName,
    setStatus,
    setExtendedStatus,
    setServiceResource,
    setParty,
    setDueAt,
    setProcess,
    setExpiresAt,
    setVisibleFrom,
    postSO,
    putSO,
    purgeSO
} from '../../common/testimports.js'

import {default as dialogToInsert} from './testdata/01-create-dialog.js';

import {getDefaultEnduserOrgNo, getDefaultEnduserSsn} from "../../common/token.js";
import {notValidEnduserId} from '../../common/config.js';

export default function () {

    const defaultResource = "urn:altinn:resource:ttd-dialogporten-automated-tests";
    const endUserId = "urn:altinn:person:identifier-no:" + getDefaultEnduserSsn();
    const updatedAfter = (new Date()).toISOString(); // We use this on all tests to avoid clashing with unrelated dialogs
    const defaultFilter = "?UpdatedAfter=" + updatedAfter;

    describe('Perform simple dialog list', () => {
        // Arrange
        let count = 10;
        let dialogIds = createDialogs(count);

        // Assert
        let r = getSO('dialogs');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf.at.least(10);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('Search for title', () => {
        // Arrange
        let titleToSearchFor = uuidv4();
        let dialogIds = createDialogs(5, (dialog, index) => {
            if (index == 3) {
                setTitle(dialog, titleToSearchFor);
            }
        });

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&ServiceResource=' + defaultResource + '&EndUserId=' + endUserId + '&Search=' + titleToSearchFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('Search for body', () => {
        // Arrange
        let additionalInfoToSearchFor = uuidv4();
        let dialogIds = createDialogs(5, (dialog, index) => {
            if (index == 3) {
                setAdditionalInfo(dialog, additionalInfoToSearchFor);
            }
        });

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&ServiceResource=' + defaultResource + '&EndUserId=' + endUserId + '&Search=' + additionalInfoToSearchFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('Search for sender name ', () => {
        // Arrange
        let senderNameToSearchFor = uuidv4();
        let dialogIds = createDialogs(5, (dialog, index) => {
            if (index == 3) {
                setSenderName(dialog, senderNameToSearchFor);
            }
        });

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&ServiceResource=' + defaultResource + '&EndUserId=' + endUserId + '&Search=' + senderNameToSearchFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('Filter by extended status', () => {
        // Arrange
        let extendedStatusToSearchFor = "status:" + uuidv4();
        let secondExtendedStatusToSearchFor = "status:" + uuidv4();
        let dialogIds = createDialogs(5, (dialog, index) => {
            if (index == 3) {
                setExtendedStatus(dialog, extendedStatusToSearchFor);
            }
            if (index == 2) {
                setExtendedStatus(dialog, secondExtendedStatusToSearchFor);
            }
        });

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&ExtendedStatus=' + extendedStatusToSearchFor + "&ExtendedStatus=" + secondExtendedStatusToSearchFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(2);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('List with limit', () => {
        // Arrange
        let dialogIds = createDialogs(10);

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&Limit=3');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(3);
        expect(r.json(), 'response json').to.have.property("hasNextPage").to.be.true;
        expect(r.json(), 'response json').to.have.property("continuationToken");

        let r2 = getSO('dialogs/' + defaultFilter + '&Limit=3&ContinuationToken=' + r.json().continuationToken);
        expectStatusFor(r2).to.equal(200);
        expect(r2, 'response').to.have.validJsonBody();
        expect(r2.json(), 'response json').to.have.property("items").with.lengthOf(3);

        // Check that we get other ids in the continuation call
        let allIds = r.json().items.concat(r2.json().items).map((item) => item.id);
        expect(allIds.some((id, i) => allIds.indexOf(id) !== i)).to.be.false;

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('List with custom orderBy', () => {
        let titleForDueAtItem = uuidv4();
        let titleForUpdatedItem = uuidv4();
        let titleForLastItem = uuidv4();

        let dialogIds = createDialogs(10, (dialog, index) => {
            if (index == 3) {
                setTitle(dialog, titleForDueAtItem);
                setDueAt(dialog, new Date("2033-12-07T10:13:00Z"));
            }
            if (index == 9) {
                setTitle(dialog, titleForLastItem);
            }
        });

        // Update single dialog
        let firstDialogId = dialogIds[0];
        let firstDialog = dialogToInsert();
        firstDialog.id = firstDialogId;
        setProcess(firstDialog, "urn:test:process:1");
        setTitle(firstDialog, titleForUpdatedItem);

        let post = putSO("dialogs/" + firstDialogId, firstDialog);
        expectStatusFor(post).to.equal(204);

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&Limit=3&OrderBy=dueAt_desc,updatedAt_desc');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(3);
        expect(r.json().items[0], 'first dialog title').to.haveContentOfType("title").that.hasLocalizedText(titleForDueAtItem);
        expect(r.json().items[1], 'second dialog title').to.haveContentOfType("title").that.hasLocalizedText(titleForUpdatedItem);
        expect(r.json().items[2], 'third dialog title').to.haveContentOfType("title").that.hasLocalizedText(titleForLastItem);

        r = getSO('dialogs/' + defaultFilter + '&Limit=3&OrderBy=dueAt_asc,updatedAt_desc');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(3);
        expect(r.json().items[0], 'first dialog reversed title').to.haveContentOfType("title").that.hasLocalizedText(titleForUpdatedItem);
        expect(r.json().items[1], 'second dialog reversed title').to.haveContentOfType("title").that.hasLocalizedText(titleForLastItem);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('List with party filter', () => {
        // Arrange
        let auxParty = "urn:altinn:organization:identifier-no:" + getDefaultEnduserOrgNo();
        let dialogIds = createDialogs(10, (dialog, index) => {
            if (index == 1) {
                setParty(dialog, auxParty);
            }
        });

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&Party=' + auxParty);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
        expect(r.json().items[0], 'party').to.have.property("party").that.equals(auxParty);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('List with resource filter', () => {
        // Arrange
        let auxResource = "urn:altinn:resource:ttd-dialogporten-automated-tests-2"; // This must exist in Resource Registry
        let dialogIds = createDialogs(10, (dialog, index) => {
            if (index == 1) {
                setServiceResource(dialog, auxResource);
            }
        });

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&ServiceResource=' + auxResource);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
        expect(r.json().items[0], 'party').to.have.property("serviceResource").that.equals(auxResource);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('List with invalid process', () => {
        // Arrange
        let dialogIds = createDialogs(10);

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&process=inval|d');
        expectStatusFor(r).to.equal(400);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("errors");

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe('List with process', () => {
        // Arrange
        let processToSeachFor = "urn:test:listsearch:1";
        let dialogIds = createDialogs(10, (dialog, index) => {
            setProcess(dialog, "urn:test:listsearch:" + (index + 1));
        });

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&process=' + processToSeachFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
        expect(r.json().items[0], 'process').to.have.property("process").that.equals(processToSeachFor);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    })

    describe('List with enduserid', () => {
        // Arrange
        let auxResource = "urn:altinn:resource:ttd-dialogporten-automated-tests-2"; // This must exist in Resource Registry
        let dialogIds = createDialogs(10, (dialog, index) => {
            if (index == 1) {
                setServiceResource(dialog, auxResource);
            }
        });

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&EndUserId=' + endUserId + '&ServiceResource=' + auxResource);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
        expect(r.json().items[0], 'party').to.have.property("serviceResource").that.equals(auxResource);

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        })
    })

    describe('List with invalid enduserid', () => {
        // Arrange
        let invalidEndUserId = "urn:altinn:person:identifier-no:" + notValidEnduserId;
        let auxResource = "urn:altinn:resource:ttd-dialogporten-automated-tests-2"; // This must exist in Resource Registry
        let dialogIds = createDialogs(10);

        // Assert
        let r = getSO('dialogs/' + defaultFilter + '&EndUserId=' + invalidEndUserId + '&ServiceResource=' + auxResource);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').not.to.have.property("items");

        // Clean up
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });
}

function createDialogs(count, modify) {
    let dialogIds = [];
    for (let i = 0; i < count; i++) {
        let d = dialogToInsert();
        setTitle(d, "e2e-test-dialog #" + (i + 1), "nn_NO");
        if (modify) {
            modify(d, i);
        }
        let r = postSO("dialogs", d);
        expectStatusFor(r).to.equal(201);
        dialogIds.push(r.json());
    }
    return dialogIds;
}

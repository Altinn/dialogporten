import {
    describe, expect, expectStatusFor,
    getEU,
    uuidv4,
    uuidv7,
    setTitle,
    setAdditionalInfo,
    setSearchTags,
    setSenderName,
    setStatus,
    setParty,
    setServiceResource,
    setExtendedStatus,
    setDueAt,
    setExpiresAt,
    setVisibleFrom,
    setProcess,
    setSystemLabel,
    postSO,
    putSO,
    purgeSO
} from '../../common/testimports.js'

import {default as dialogToInsert} from '../serviceowner/testdata/01-create-dialog.js';
import {getDefaultEnduserOrgNo, getDefaultEnduserSsn} from '../../common/token.js';
import {sleep} from 'k6';

export default function () {

    let dialogs = [];
    let dialogIds = [];

    let titleToSearchFor = uuidv4();
    let processToSearchFor = "urn:test:process:1";
    let systemLabelToSearchFor = 'Bin';
    let additionalInfoToSearchFor = uuidv4();
    let searchTagsToSearchFor = [uuidv4(), uuidv4()];
    let extendedStatusToSearchFor = "status:" + uuidv4();
    let secondExtendedStatusToSearchFor = "status:" + uuidv4();
    let senderNameToSearchFor = uuidv4()
    let defaultParty = "urn:altinn:person:identifier-no:" + getDefaultEnduserSsn();
    let auxParty = "urn:altinn:organization:identifier-no:" + getDefaultEnduserOrgNo(); // some party that we can access
    let auxResource = "urn:altinn:resource:ttd-dialogporten-automated-tests-2"; // Note! We assume that this exists!
    let titleForDueAtItem = "due_" + uuidv4();
    let titleForExpiresAtItem = "expires_" + uuidv4();
    let titleForUpdatedItem = "updated_" + uuidv4();
    let titleForLastItem = "last_" + uuidv4();
    let idForCustomOrg = uuidv7();
    let updatedAfter = (new Date()).toISOString(); // We use this on all tests to avoid clashing with unrelated dialogs
    let defaultFilter = "?UpdatedAfter=" + updatedAfter + "&Party=" + defaultParty;
    let auxOrg = "ttd";

    describe('Arrange: Create some dialogs to test against', () => {
        for (let i = 0; i < 15; i++) {
            let d = dialogToInsert();
            setTitle(d, "e2e-test-dialog eu #" + (i + 1), "nn_NO");
            setProcess(d, ("urn:test:process:" + (i + 1)));
            setParty(d, defaultParty);
            setVisibleFrom(d, null);
            dialogs.push(d);
        }

        let d = -1;
        setTitle(dialogs[++d], titleToSearchFor);
        setAdditionalInfo(dialogs[++d], additionalInfoToSearchFor);
        setSearchTags(dialogs[++d], searchTagsToSearchFor);
        setStatus(dialogs[++d], "draft");
        setExtendedStatus(dialogs[++d], extendedStatusToSearchFor);

        setSenderName(dialogs[++d], senderNameToSearchFor);
        setExtendedStatus(dialogs[d], secondExtendedStatusToSearchFor);

        setServiceResource(dialogs[++d], auxResource);
        setParty(dialogs[++d], auxParty);

        setTitle(dialogs[++d], titleForDueAtItem);
        setDueAt(dialogs[d], new Date("2033-12-07T10:13:00Z"));

        setTitle(dialogs[++d], titleForExpiresAtItem);
        setExpiresAt(dialogs[d], new Date("2034-03-07T10:13:00Z"));
        setSystemLabel(dialogs[d], 'Bin')

        dialogs[++d].id = idForCustomOrg;

        setTitle(dialogs[dialogs.length - 1], titleForLastItem);

        let tokenOptions = {};
        dialogs.forEach((d) => {
            tokenOptions = (d.id == idForCustomOrg) ? {orgName: auxOrg} : {};
            let r = postSO("dialogs", d, null, tokenOptions);
            expectStatusFor(r).to.equal(201);
            dialogIds.push(r.json());
        });

        let penultimateDialog = dialogs[dialogs.length - 2];
        let penultimateDialogId = dialogIds[dialogIds.length - 2];
        setTitle(penultimateDialog, titleForUpdatedItem);
        let r = putSO("dialogs/" + penultimateDialogId, penultimateDialog);
        expectStatusFor(r).to.equal(204);

    });

    describe('Check seen logs in dialog list', () => {
        // Trigger seen logs
        getEU('dialogs/' + dialogIds[0]);
        getEU('dialogs/' + dialogIds[1]);

        let r = getEU('dialogs/' + defaultFilter);

        let searchResult = r.json();
        expect(searchResult.items, 'items').to.be.an('array').that.is.not.empty;

        let d1 = searchResult.items.find((d) => d.id == dialogIds[0]);
        expect(d1.seenSinceLastUpdate, 'seenSinceLastUpdate').to.have.lengthOf(1);
        expect(d1.seenSinceLastUpdate[0].seenBy.actorId, 'actorId').to.match(/urn:altinn:person:identifier-ephemeral/);
        expect(d1.seenSinceLastUpdate[0].isCurrentEndUser, 'isCurrentEndUser').to.equal(true);

        let d2 = r.json().items.find((d) => d.id == dialogIds[1]);
        expect(d2.seenSinceLastUpdate, 'seenSinceLastUpdate').to.have.lengthOf(1);
        expect(d1.seenSinceLastUpdate[0].seenBy.actorId, 'actorId').to.match(/urn:altinn:person:identifier-ephemeral/);
        expect(d2.seenSinceLastUpdate[0].isCurrentEndUser, 'isCurrentEndUser').to.equal(true);
    });


    describe('Perform simple dialog list', () => {
        let r = getEU('dialogs' + defaultFilter);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf.at.least(10);
    });

    describe('Search for title', () => {
        sleep(2); // wait for indexing
        let r = getEU('dialogs/' + defaultFilter + '&Search=' + titleToSearchFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
    });

    describe('Search for body', () => {
        let r = getEU('dialogs/' + defaultFilter + '&Search=' + additionalInfoToSearchFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
    });

    describe('Search for sender name', () => {
        let r = getEU('dialogs/' + defaultFilter + '&Search=' + senderNameToSearchFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
    });

    describe('Filter by extended status', () => {
        let r = getEU('dialogs/' + defaultFilter + '&ExtendedStatus=' + extendedStatusToSearchFor + "&ExtendedStatus=" + secondExtendedStatusToSearchFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(2);
    });

    describe('List with limit', () => {
        let r = getEU('dialogs/' + defaultFilter + '&Limit=3');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(3);
        expect(r.json(), 'response json').to.have.property("hasNextPage").to.be.true;
        expect(r.json(), 'response json').to.have.property("continuationToken");

        let r2 = getEU('dialogs/' + defaultFilter + '&Limit=3&ContinuationToken=' + r.json().continuationToken);
        expectStatusFor(r2).to.equal(200);
        expect(r2, 'response').to.have.validJsonBody();
        expect(r2.json(), 'response json').to.have.property("items").with.lengthOf(3);

        // Check that we get other ids in the continuation call
        let allIds = r.json().items.concat(r2.json().items).map((item) => item.id);
        expect(allIds.some((id, i) => allIds.indexOf(id) !== i)).to.be.false;
    });

    describe('List with custom orderBy', () => {
        let r = getEU('dialogs/' + defaultFilter + '&Limit=3&OrderBy=dueAt_desc,updatedAt_desc');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(3);
        expect(r.json().items[0], 'first dialog').to.have.haveContentOfType("title").that.hasLocalizedText(titleForDueAtItem);
        expect(r.json().items[1], 'second dialog').to.have.haveContentOfType("title").that.hasLocalizedText(titleForUpdatedItem);
        expect(r.json().items[2], 'third dialog').to.have.haveContentOfType("title").that.hasLocalizedText(titleForLastItem);

        r = getEU('dialogs/' + defaultFilter + '&Limit=3&OrderBy=dueAt_asc,updatedAt_desc');
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(3);
        expect(r.json().items[0], 'first dialog reversed').to.have.haveContentOfType("title").that.hasLocalizedText(titleForUpdatedItem);
        expect(r.json().items[1], 'second dialog reversed').to.have.haveContentOfType("title").that.hasLocalizedText(titleForLastItem);
    });

    describe('List with resource filter', () => {
        let r = getEU('dialogs/' + defaultFilter + '&ServiceResource=' + auxResource);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
        expect(r.json().items[0], 'party').to.have.property("serviceResource").that.equals(auxResource);
    });

    describe('List with invalid process filter', () => {
        let r = getEU('dialogs/' + defaultFilter + '&process=inval|d');
        expectStatusFor(r).to.equal(400);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("errors");
        expect(r.json().errors, 'errors').to.have.property("process").with.lengthOf(1);
    })

    describe('List with process filter', () => {
        let r = getEU('dialogs/' + defaultFilter + '&process=' + processToSearchFor);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
        expect(r.json().items[0], 'process').to.have.property("process").that.equals(processToSearchFor);
    })

    describe('List with systemLabel filter', () => {
        let response = getEU('dialogs/' + defaultFilter + '&SystemLabel=' + systemLabelToSearchFor);
        expectStatusFor(response).to.equal(200);
        expect(response, 'response').to.have.validJsonBody();
        expect(response.json(), 'response json').to.have.property("items").with.lengthOf(1);
        expect(response.json().items[0].endUserContext.systemLabels).to.be.an('array').that.includes(systemLabelToSearchFor);
    })

    describe('List with invalid systemLabel filter', () => {
        let response = getEU('dialogs/' + defaultFilter + '&SystemLabel=' + 'firepit');
        expectStatusFor(response).to.equal(400);
        expect(response, 'response').to.have.validJsonBody();
        expect(response.json(), 'response json').to.have.property("errors");
    })
    /*
    Disabled for now. Dialogporten doesn't have proper TTD handling as of yet.

    describe('List with org filter', () => {
        console.log('dialogs/' + defaultFilter + '&Org=' + auxOrg);
        let r = getEU('dialogs/' + defaultFilter + '&Org=' + auxOrg);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.have.property("items").with.lengthOf(1);
        expect(r.json().items[0], 'org').to.have.property("org").that.equals(auxOrg);
    });
    */

    describe("Cleanup", () => {
        dialogIds.forEach((d) => {
            let r = purgeSO("dialogs/" + d);
            expect(r.status, 'response status').to.equal(204);
        });
    });

    describe("Check if we get 404 Not found", () => {
        let r = getEU('dialogs/' + dialogIds[0]);
        expectStatusFor(r).to.equal(404);
    });
}

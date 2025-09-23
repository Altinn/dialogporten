import {
    describe,
    setVisibleFrom,
    expect,
    expectStatusFor,
    getSO,
    postSO,
    purgeSO,
    getEU
} from '../../common/testimports.js'
import {default as dialogToInsert} from '../serviceowner/testdata/01-create-dialog.js';

export default function () {
    let dialogId = null;

    describe('Perform dialog create', () => {
        let dialog = dialogToInsert();
        setVisibleFrom(dialog, null);
        dialog.content.Title.value = [
            {
                "languageCode": "en",
                "value": "en-title-content"
            },
            {
                "languageCode": "nb",
                "value": "nb-title-content"
            }
        ];
        dialog.content.ExtendedStatus.Value = [
            {
                "languageCode": "nb",
                "value": "nb-Status-content"
            }
        ];
        dialog.content.Summary.Value = [
            {
                "languageCode": "it",
                "value": "it-summary-content"
            },
            {
                "languageCode": "fr",
                "value": "fr-summary-content"
            }
        ];


        let r = postSO("dialogs", dialog);

        expectStatusFor(r).to.equal(201);
        dialogId = r.json();
    });

    describe('Perform dialog get with nb accept-language', () => {
        let header = createAcceptLanguageHeader("nb");
        let r = getEU('dialogs/' + dialogId, header);
        expectStatusFor(r).to.equal(200);

        let dialog = r.json();
        expect(dialog.content.title.value.length).to.equal(1);
        expect(dialog.content.title.value[0].languageCode).to.equal("nb");

        expect(dialog.content.summary.value.length).to.equal(2);


        expect(dialog.content.extendedStatus.value.length).to.equal(1);
        expect(dialog.content.extendedStatus.value[0].languageCode).to.equal("nb");

    });


    describe('Perform dialog get with invalid accept-language', () => {
        let header = createAcceptLanguageHeader("it;a=1.0, nb");
        let r = getEU('dialogs/' + dialogId, header);
        expectStatusFor(r).to.equal(400);
        console.log(r.json())
    });

    describe('Perform dialog get with sv accept-language', () => {
        let header = createAcceptLanguageHeader("sv");
        let r = getEU('dialogs/' + dialogId, header);
        expectStatusFor(r).to.equal(200);

        let dialog = r.json();
        expect(dialog.content.title.value.length).to.equal(1);
        expect(dialog.content.title.value[0].languageCode).to.equal("nb");

        expect(dialog.content.summary.value.length).to.equal(2);

        expect(dialog.content.extendedStatus.value.length).to.equal(1);
        expect(dialog.content.extendedStatus.value[0].languageCode).to.equal("nb");

    });

    describe('Perform dialog get with da accept-language', () => {
        let header = createAcceptLanguageHeader("da");
        let r = getEU('dialogs/' + dialogId, header);
        expectStatusFor(r).to.equal(200);

        let dialog = r.json();
        expect(dialog.content.title.value.length).to.equal(1);
        expect(dialog.content.title.value[0].languageCode).to.equal("nb");

        expect(dialog.content.summary.value.length).to.equal(2);

        expect(dialog.content.extendedStatus.value.length).to.equal(1);
        expect(dialog.content.extendedStatus.value[0].languageCode).to.equal("nb");

    });


    describe('Perform dialog get with * accept-language', () => {
        let header = createAcceptLanguageHeader("*");
        let r = getEU('dialogs/' + dialogId, header);
        expectStatusFor(r).to.equal(200);

        let dialog = r.json();
        expect(dialog.content.title.value.length).to.equal(1);
        expect(dialog.content.title.value[0].languageCode).to.equal("en");

        expect(dialog.content.summary.value.length).to.equal(2);

        expect(dialog.content.extendedStatus.value.length).to.equal(1);
        expect(dialog.content.extendedStatus.value[0].languageCode).to.equal("nb");
    });


    describe('Perform dialog get with it accept-language', () => {
        let header = createAcceptLanguageHeader("it");
        let r = getEU('dialogs/' + dialogId, header);
        expectStatusFor(r).to.equal(200);

        let dialog = r.json();
        expect(dialog.content.title.value.length).to.equal(1);
        expect(dialog.content.title.value[0].languageCode).to.equal("en");

        expect(dialog.content.summary.value.length).to.equal(1);
        expect(dialog.content.summary.value[0].languageCode).to.equal("it");

        expect(dialog.content.extendedStatus.value.length).to.equal(1);
        expect(dialog.content.extendedStatus.value[0].languageCode).to.equal("nb");
    });

    describe('Purge dialogs', () => {
        let r = purgeSO('dialogs/' + dialogId);
        expectStatusFor(r).to.equal(204);
    });
}

function createAcceptLanguageHeader(headerCode) {

    return {"headers": {"Accept-Language": headerCode}}
}

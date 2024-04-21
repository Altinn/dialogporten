export { textSummary } from 'https://jslib.k6.io/k6-summary/0.1.0/index.js';

export function generateJUnitXML(k6Json) {
    const xmlDoc = [];
    xmlDoc.push('<?xml version="1.0" encoding="UTF-8" ?>');
    xmlDoc.push('<testsuites>');

    function xmlEncode(string) {
        return string.replace(/&/g, '&amp;')
                     .replace(/</g, '&lt;')
                     .replace(/>/g, '&gt;')
                     .replace(/"/g, '&quot;')
                     .replace(/'/g, '&apos;');
    }

    function processGroup(group) {
        if (group.name) { // skip root group

            const checkName = xmlEncode(check.name);
            const groupName = xmlEncode(group.name);

            xmlDoc.push(`<testsuite name="${groupName}" tests="${group.checks.length}">`);

            group.checks.forEach(check => {
                let failed = check.fails > 0 ;
                xmlDoc.push(`<testcase classname="${groupName}" name="${checkName}">`);
                if (failed) {
                    xmlDoc.push(`<failure message="Check failed. See output K6 task for more details.">${checkName}</failure>`);
                }
                xmlDoc.push('</testcase>');
            });
        }

        group.groups.forEach(subGroup => {
            processGroup(subGroup);
        });

        xmlDoc.push('</testsuite>');
    }

    processGroup(k6Json.root_group);

    xmlDoc.push('</testsuites>');
    return xmlDoc.join('\n');
}

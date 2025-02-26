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
        if (group.name && group.checks.length) {
            const groupName = xmlEncode(group.name);
            xmlDoc.push(`<testsuite name="${groupName}" tests="${group.checks.length}">`);

            group.checks.forEach(check => {
                const checkName = xmlEncode(check.name);
                let failed = check.fails > 0 ;
                xmlDoc.push(`<testcase classname="${groupName}" name="${checkName}">`);
                if (failed) {
                    xmlDoc.push(`<failure message="Check failed. See output K6 task for more details.">${checkName}</failure>`);
                }
                xmlDoc.push('</testcase>');
            });

            xmlDoc.push('</testsuite>');
        }

        group.groups.forEach(subGroup => {
            processGroup(subGroup);
        });

    }

    processGroup(k6Json.root_group);

    xmlDoc.push('</testsuites>');
    return xmlDoc.join('\n');
}

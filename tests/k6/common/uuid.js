export { uuidv4 } from './k6-utils.js';

export function uuidv7() {

    let uuid = "";

    // generate time chars
    let milli = (new Date()).getTime();
    let time = hex(milli, 12);

    // cat time and random chars
    uuid += time.substring(0, 8);
    uuid += "-";
    uuid += time.substring(8, 12);
    uuid += "-";
    uuid += hex(random(16), 4);
    uuid += "-";
    uuid += hex(random(16), 4);
    uuid += "-";
    uuid += hex(random(48), 12);

    // version and variant
    uuid = uuid.split('');
    uuid[14] = '7';
    uuid[19] = ['8', '9', 'a', 'b'][random(2)];
    uuid = uuid.join('');

    return uuid;
}

function hex(number, len) {
    return number.toString(16).padStart(len, '0');
}

function random(bits) {
    if (bits > 52) { bits = 52 };
    return Math.floor(Math.random() * Math.pow(2, bits));
}

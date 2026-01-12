/**
 * This file contains the implementation of reading test data from CSV files.
 * The test data includes service owners, end users, and end users with tokens.
 * The data is read using the PapaParse library and stored in SharedArray variables.
 *
 * @module readTestdata
 */

import http from 'k6/http';
import papaparse from 'https://jslib.k6.io/papaparse/5.1.1/index.js';
import { SharedArray } from 'k6/data';
import exec from 'k6/execution';


/**
 * Function to parse CSV data from a string.
 * @param {string} csvData - The CSV data as a string.
 * @returns {Array} - Parsed data as an array of objects.
 */
function parseCsvData(csvData) {
  try {
    return papaparse.parse(csvData, { header: true, skipEmptyLines: true }).data;
  } catch (error) {
    console.log(`Error reading CSV file: ${error}`);
    return [];
  }
}

/**
 * Function to read the CSV file specified by the filename parameter.
 * @param {} filename
 * @returns
 */
function readCsv(filename) {
  try {
    return papaparse.parse(open(filename), { header: true, skipEmptyLines: true }).data;
  } catch (error) {
    console.log(`Error reading CSV file: ${error}`);
    return [];
  }
}

if (!__ENV.API_ENVIRONMENT) {
  throw new Error('API_ENVIRONMENT must be set');
}
const filenameEndusers = `../performancetest_data/endusers-${__ENV.API_ENVIRONMENT}.csv`;
const filenameServiceowners = `../performancetest_data/serviceowners-${__ENV.API_ENVIRONMENT}.csv`;
const filenameDialogsWithTransmissions = `../performancetest_data/dialogs-with-transmissions-${__ENV.API_ENVIRONMENT}.csv`;

/**
 * SharedArray variable that stores the service owners data.
 * The data is parsed from the CSV file specified by the filenameServiceowners variable.
 *
 * @name serviceOwners
 * @type {SharedArray}
 */
export const serviceOwners = new SharedArray('serviceOwners', function () {
  return readCsv(filenameServiceowners);
});

/**
 * SharedArray variable that stores the end users data.
 * The data is parsed from the CSV file specified by the filenameEndusers variable.
 * The filenameEndusers variable is dynamically generated based on the value of the API_ENVIRONMENT environment variable.
 *
 * @name endUsers
 * @type {SharedArray}
 */
export const endUsers = new SharedArray('endUsers', function () {
  return readCsv(filenameEndusers);
});

/**
 * Reads the enduser data from github raw CSV file.
 * @returns {Array} endUsers - Array of end users read from CSV file.
 */
export function getParties() {
  let env = __ENV.API_ENVIRONMENT;
  if (env === 'localdev' || env === 'localdev_docker') {
    env = 'yt01';
  }
  const res = http.get(`https://raw.githubusercontent.com/Altinn/dialogporten/refs/heads/main/tests/k6/tests/performancetest_data/parties-${env}.csv`);
  return parseCsvData(res.body);
}

export const dialogsWithTransmissions = new SharedArray('dialogsWithTransmissions', function () {
  return readCsv(filenameDialogsWithTransmissions);
});

export function endUsersPart(totalVus, vuId) {
  const endUsersLength = endUsers.length;
  if (totalVus == 1) {
      return endUsers.slice(0, endUsersLength);
  }
  let usersPerVU = Math.floor(endUsersLength / totalVus);
  let extras = endUsersLength % totalVus;
  let ixStart = (vuId-1) * usersPerVU;
  if (vuId <= extras) {
      usersPerVU++;
      ixStart += vuId - 1;
  }
  else {
      ixStart += extras;
  }
  return endUsers.slice(ixStart, ixStart + usersPerVU);
}

export function setup() {
  const totalVus = exec.test.options.scenarios.default.vus ?? __ENV.stages_target ?? 10;
  let parts = [];
  for (let i = 1; i <= totalVus; i++) {
      if (i <= endUsers.length) {
        parts.push(endUsersPart(totalVus, i));
      }
  }
  return parts;
}

export function validateTestData(data, serviceOwners=null) {
    const ix = (exec.vu.idInTest - 1) % data.length;
    if (!Array.isArray(data) || !data[ix]) {
        throw new Error('Invalid data structure: expected array of end users');
    }
    
    const myEndUsers = data[ix];
    if (!Array.isArray(myEndUsers) || myEndUsers.length === 0) {
        throw new Error('Invalid end users array: expected non-empty array');
    }
    if (serviceOwners !== null) {
        if (!Array.isArray(serviceOwners) || serviceOwners.length === 0) {
            throw new Error('Invalid service owners array: expected non-empty array');
        }
    }
    return {
        endUsers: myEndUsers,
        index: exec.vu.iterationInInstance % myEndUsers.length
    };
}

export const texts = [ "påkrevd", "rapportering", "sammendrag", "Utvidet Status", "ingen HTML-støtte", "et eller annet", "Skjema", "Skjema for rapportering av et eller annet", "Maks 200 tegn", "liste" ];
export const texts_no_hit = [ "sjøvegan", "larvik", "kvalsund", "jøssheim", "sørli"];
export const resources = [ 
    "ttd-dialogporten-performance-test-01", 
    "ttd-dialogporten-performance-test-02", 
    "ttd-dialogporten-performance-test-03", 
    "ttd-dialogporten-performance-test-04", 
    "ttd-dialogporten-performance-test-05", 
    "ttd-dialogporten-performance-test-06", 
    "ttd-dialogporten-performance-test-07", 
    "ttd-dialogporten-performance-test-08", 
    "ttd-dialogporten-performance-test-09", 
    "ttd-dialogporten-performance-test-10"
];

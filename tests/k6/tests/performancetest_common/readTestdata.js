/**
 * This file contains the implementation of reading test data from CSV files.
 * The test data includes service owners, end users, and end users with tokens.
 * The data is read using the PapaParse library and stored in SharedArray variables.
 * 
 * @module readTestdata
 */

import papaparse from 'https://jslib.k6.io/papaparse/5.1.1/index.js';
import { SharedArray } from 'k6/data';
import exec from 'k6/execution';
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
  const totalVus = exec.test.options.scenarios.default.vus;
  let parts = [];
  for (let i = 1; i <= totalVus; i++) {
      parts.push(endUsersPart(totalVus, i));
  }
  return parts;
}

export function validateTestData(data, serviceOwners=null) {
    if (!Array.isArray(data) || !data[exec.vu.idInTest - 1]) {
        throw new Error('Invalid data structure: expected array of end users');
    }
    const myEndUsers = data[exec.vu.idInTest - 1];
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



import sse from "k6/x/sse";
import { randomItem } from '../../../common/k6-utils.js';
import { getEndUserTokens } from '../../../common/token.js';
import { createBody } from './graphqlCommonFunctions.js';
import { postGQ, putSO } from '../../../common/request.js';
import { getGraphqlRequestBodyForDialogById } from '../../performancetest_data/graphql-queries.js';
import { serviceOwners } from '../../performancetest_common/readTestdata.js';
import { getEnterpriseToken } from '../../performancetest_common/getTokens.js';
import { sleep } from "k6";
import { Counter } from 'k6/metrics';
import exec from 'k6/execution';

const serverVus = __ENV.SERVER_VUS ? parseInt(__ENV.SERVER_VUS) : 10;
const clientVus = __ENV.CLIENT_VUS ? parseInt(__ENV.CLIENT_VUS) : 100;
const duration = __ENV.DURATION ? __ENV.DURATION : '1m';
const waitBeforeStart = __ENV.WAIT_BEFORE_START ? __ENV.WAIT_BEFORE_START : '30s';
const sleepBetweenUpdates = __ENV.SLEEP_BETWEEN_UPDATES ? parseInt(__ENV.SLEEP_BETWEEN_UPDATES) : 1;
const no_of_endusers = __ENV.NUMBER_OF_ENDUSERS ? parseInt(__ENV.NUMBER_OF_ENDUSERS) : 200;

export const eventsReceived = new Counter('events_received');
export const eventsSent = new Counter('successful_updates');
export const eventsFailed = new Counter('failed_updates');
export const streamsOpened = new Counter('streams_opened');
export const streamsFailed = new Counter('streams_failed');

export let options = {
  scenarios: {
    clients: {
      executor: 'constant-vus',
      exec: 'runClients',
      duration: duration + waitBeforeStart,
      vus: clientVus
    },
    server: {
      executor: 'constant-vus',
      exec: 'runUpdater',
      duration: duration,
      vus: serverVus,
      startTime: waitBeforeStart
    },
  },
  setupTimeout: '10m',
};

export function setup() {
  const tokenOptions = {
    scopes:
      'digdir:dialogporten.noconsent openid altinn:portal/enduser altinn:instances.read',
  };
  const endusers = getEndUserTokens(no_of_endusers, tokenOptions);
  const data = []
  for (const enduser in endusers) {
    const token = endusers[enduser];
    const [dialogId, dialogToken, status, title] = getDialogIdAndToken(enduser, token);
    console.log(`Enduser: ${enduser}, dialogId: ${dialogId}`);
    if (dialogId && dialogToken) {
      data.push({ 
        enduser: enduser, 
        token: token, 
        dialogId: dialogId, 
        dialogToken: dialogToken,
        status: "new",
        title: title
      });
    }
  }
  return data;
}

export function teardown(data) {
  for (const item of data) {
    updateDialog(item, true);
  }
}

export function runUpdater(data) {
  const item = randomItem(data);
  updateDialog(item);
  sleep(sleepBetweenUpdates); 
}

export function runClients(data) {
  const item = randomItem(data);
  open_sse(item.dialogId, item.token, item.dialogToken, __VU);  
}

function getDialogIdAndToken(enduser, token) {
  const paramsWithToken = {
    headers: {
      Authorization: "Bearer " + token,
      Accept: 'application/json',
      'User-Agent': 'dialogporten-k6',
    },
    tags: { name: "graphql-getAllDialogsForEnduser" }
  }
  const body = createBody(enduser, "getAllDialogsForEnduser");
  let r = postGQ(body, paramsWithToken);
  const dialogs = r.json();
  if (dialogs && dialogs.data.searchDialogs.items.length > 0) {
    const dialog = randomItem(dialogs.data.searchDialogs.items);
    const dialogId = dialog.id;
    const dialogBody = getGraphqlRequestBodyForDialogById(dialogId)
    r = postGQ(dialogBody, paramsWithToken);
    const dialogDetails = r.json();
    const title = dialogDetails.data.dialogById.dialog.content.title;
    const status = dialogDetails.data.dialogById.dialog.status;

    const dialogToken = dialogDetails.data.dialogById.dialog.dialogToken;
    return [dialogId, dialogToken, status, title];
  } else {  
    console.warn(`No dialogs found for enduser ${enduser}`);
    return [null, null, null, null];
  }
}

function updateDialog(item, restoring=false) {
  const paramsWithToken = {
    headers: {
      Authorization: "Bearer " + getEnterpriseToken(serviceOwners[0]),
      Accept: 'application/json',
      'User-Agent': 'dialogporten-k6',
    },
    tags: { name: "graphql-updateDialogStatus" }
  }
  const mutationBody = {
    "status": item.status,
    "ExpiresAt": "2125-02-13T23:16:51.9421070Z",
    "content": {
        "title": 
        JSON.parse(JSON.stringify(item.title))
    }
  };
  if (!restoring) {
    mutationBody.content.title.value[0].value += " - " + new Date().toISOString();
  }
  const resp = putSO(`dialogs/${item.dialogId}`, mutationBody, paramsWithToken)
  if (resp.status === 204) {
    if (!restoring) {
      eventsSent.add(1, { vu: String(exec.vu.idInInstance) });
    }
  } else {
    if (!restoring) {
      eventsFailed.add(1, { vu: String(exec.vu.idInInstance) });
    }
    console.error(`Failed to update dialog ${item.dialogId} for enduser ${item.enduser}. Status: ${resp.status}`);
    console.log(`Response body: ${resp.body}`);
  }
}


function open_sse(dialogId, token, dialogToken, i) {
  const url =
    'https://platform.yt01.altinn.cloud/dialogporten/graphql/stream?dialogId=' +
    dialogId;
  const body = JSON.stringify({
    operationName: 'sub',
    query: `subscription sub { dialogEvents(dialogId: \"${dialogId}\") { id type } }`,
    variables: {},
  }); 
  const params = {
      method: 'POST',
      body: body,
      headers: {
          'Content-Type': 'application/json; charset=utf-8',
          Accept: 'text/event-stream',
          Authorization: `Bearer ${dialogToken}`,
      },
      tags: {"my_k6s_tag": "hello sse"}
  }

  const response = sse.open(url, params, function (client) {
      client.on('open', function open() {
          streamsOpened.add(1, { vu: String(exec.vu.idInInstance) });
      })

      client.on('event', function (event) {
        if (event.name && event.name === 'next') {
          //console.log(`stream ${i} received event id=${event.id}, name=${event.name}, data=${event.data}`)
          eventsReceived.add(1, { vu: String(exec.vu.idInInstance) });
          if (parseInt(event.id) === 4) {
              client.close()
          }
        }
      })

      client.on('error', function (e) {
          eventsFailed.add(1, { vu: String(exec.vu.idInInstance) });
          console.log('An unexpected error occurred: ', e.error())
      })
  })
  return response;
}
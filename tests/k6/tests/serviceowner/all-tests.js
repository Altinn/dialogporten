// This file is generated, see "scripts" directory
import { default as apionly-tests } from './apionly-tests.js';
import { default as authentication } from './authentication.js';
import { default as authorization } from './authorization.js';
import { default as concurrency } from './concurrency.js';
import { default as dialogApiOnly } from './dialogApiOnly.js';
import { default as dialogCreateActivity } from './dialogCreateActivity.js';
import { default as dialogCreateExternalResource } from './dialogCreateExternalResource.js';
import { default as dialogCreateIdempotentKey } from './dialogCreateIdempotentKey.js';
import { default as dialogCreateInvalidActionCount } from './dialogCreateInvalidActionCount.js';
import { default as dialogCreateInvalidProcess } from './dialogCreateInvalidProcess.js';
import { default as dialogCreatePatchDelete } from './dialogCreatePatchDelete.js';
import { default as dialogCreateUpdatePatchDeleteCorrespondenceResource } from './dialogCreateUpdatePatchDeleteCorrespondenceResource.js';
import { default as dialogDetails } from './dialogDetails.js';
import { default as dialogRestore } from './dialogRestore.js';
import { default as dialogSearch } from './dialogSearch.js';
import { default as dialogUpdateActivity } from './dialogUpdateActivity.js';
import { default as dialogUpdateApiOnly } from './dialogUpdateApiOnly.js';

export default function() {
  apionly-tests();
  authentication();
  authorization();
  concurrency();
  dialogApiOnly();
  dialogCreateActivity();
  dialogCreateExternalResource();
  dialogCreateIdempotentKey();
  dialogCreateInvalidActionCount();
  dialogCreateInvalidProcess();
  dialogCreatePatchDelete();
  dialogCreateUpdatePatchDeleteCorrespondenceResource();
  dialogDetails();
  dialogRestore();
  dialogSearch();
  dialogUpdateActivity();
  dialogUpdateApiOnly();
}

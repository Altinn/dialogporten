# Changelog

## [1.47.1](https://github.com/Altinn/dialogporten/compare/v1.47.0...v1.47.1) (2025-01-22)


### Bug Fixes

* **service:** Set minimum log level Information for ConsoleLogEventBus ([#1725](https://github.com/Altinn/dialogporten/issues/1725)) ([247a325](https://github.com/Altinn/dialogporten/commit/247a32503c62fea09048bddf3debc80b1a3f663a))

## [1.47.0](https://github.com/Altinn/dialogporten/compare/v1.46.0...v1.47.0) (2025-01-22)


### Features

* Manual release ([#1723](https://github.com/Altinn/dialogporten/issues/1723)) ([6d093d1](https://github.com/Altinn/dialogporten/commit/6d093d1b5fde84f8e58546d6a64dd553cb3eddab))

## [1.46.0](https://github.com/Altinn/dialogporten/compare/v1.45.1...v1.46.0) (2025-01-21)


### Features

* **webapi:** Add option to disable Altinn event generation ([#1633](https://github.com/Altinn/dialogporten/issues/1633)) ([dda7c1f](https://github.com/Altinn/dialogporten/commit/dda7c1f8ece73c092a62fbe1bae42bb553b1e1d5))

## [1.45.1](https://github.com/Altinn/dialogporten/compare/v1.45.0...v1.45.1) (2025-01-18)


### Bug Fixes

* **graphql:** Add missing search parameters for paging and sorting ([#1671](https://github.com/Altinn/dialogporten/issues/1671)) ([02f2335](https://github.com/Altinn/dialogporten/commit/02f2335d7eb2dde1e0a6e95e5ccc9918b1b15b34))
* Removed .AsSingleQuery from EndUser Search query ([#1707](https://github.com/Altinn/dialogporten/issues/1707)) ([2a3153b](https://github.com/Altinn/dialogporten/commit/2a3153b216f6a9e02ebef4d343d52f7b83cd248d))
* **webapi:** Use correct language code for norwegian in OpenApi description ([#1705](https://github.com/Altinn/dialogporten/issues/1705)) ([ce0a07d](https://github.com/Altinn/dialogporten/commit/ce0a07d4622839a5f1c3f467ab83950d7750d49e))

## [1.45.0](https://github.com/Altinn/dialogporten/compare/v1.44.2...v1.45.0) (2025-01-15)


### Features

* added id to attachments, ApiActions and GuiActions in DialogCreate ([#1670](https://github.com/Altinn/dialogporten/issues/1670)) ([470e5a9](https://github.com/Altinn/dialogporten/commit/470e5a916c331f31b4015a3847d566c5d99276da))
* **apps:** export logs to open telemetry endpoint ([#1617](https://github.com/Altinn/dialogporten/issues/1617)) ([1a71763](https://github.com/Altinn/dialogporten/commit/1a71763647b92fe7780dd7982c6b2f00f4d0d50e))
* **janitor:** add otlp logger for janitor ([#1686](https://github.com/Altinn/dialogporten/issues/1686)) ([2e1656b](https://github.com/Altinn/dialogporten/commit/2e1656b787eb0d47142b92e2453215e47a6760f3))


### Bug Fixes

* **app:** Add missing telemetry setup GraphQL and Service ([#1695](https://github.com/Altinn/dialogporten/issues/1695)) ([601a826](https://github.com/Altinn/dialogporten/commit/601a8268a4763c87925bffe3352297edd1e191d0))
* Authentication level claim is 0 in dialog token ([#1654](https://github.com/Altinn/dialogporten/issues/1654)) ([37e545a](https://github.com/Altinn/dialogporten/commit/37e545a0da0c1c5d354c7b2cb8ab4ca163a2bf17))
* **graphql:** Add missing activity types ([#1684](https://github.com/Altinn/dialogporten/issues/1684)) ([a0697ae](https://github.com/Altinn/dialogporten/commit/a0697aee2c850156df25503b42bd667377cc6aab))
* **graphql:** Set max execution depth to allow inspection query ([#1679](https://github.com/Altinn/dialogporten/issues/1679)) ([6265110](https://github.com/Altinn/dialogporten/commit/62651109ce308be85b92495c6a4a8bf5f4decf6c)), closes [#1680](https://github.com/Altinn/dialogporten/issues/1680)
* **web-api:** re-enable health checks ([#1681](https://github.com/Altinn/dialogporten/issues/1681)) ([96c2c3e](https://github.com/Altinn/dialogporten/commit/96c2c3e8d3e7de98bb4ec5ae0eba08d713598987))

## [1.44.2](https://github.com/digdir/dialogporten/compare/v1.44.1...v1.44.2) (2025-01-08)


### Bug Fixes

* **webi:** Add missing type on ETag response headers ([#1666](https://github.com/digdir/dialogporten/issues/1666)) ([df559ed](https://github.com/digdir/dialogporten/commit/df559ed7f7a1a09a1a3771dd5cc9c3526d781e3e))

## [1.44.1](https://github.com/digdir/dialogporten/compare/v1.44.0...v1.44.1) (2025-01-07)


### Bug Fixes

* **ci:** Use correct size for yt01 db ([#1658](https://github.com/digdir/dialogporten/issues/1658)) ([e18e5f7](https://github.com/digdir/dialogporten/commit/e18e5f7e44556e1a4173906303ccef14aeb9de13))

## [1.44.0](https://github.com/digdir/dialogporten/compare/v1.43.0...v1.44.0) (2025-01-07)


### Features

* **webapi:** Add ETag to response headers ([#1645](https://github.com/digdir/dialogporten/issues/1645)) ([7a32e60](https://github.com/digdir/dialogporten/commit/7a32e601061b42400aa1c94b61be69ff7c9d0ec9))


### Bug Fixes

* disable slack notifier ([#1655](https://github.com/digdir/dialogporten/issues/1655)) ([554fc8b](https://github.com/digdir/dialogporten/commit/554fc8b3294c125b0e8561ebcbfe254e75fede1c))

## [1.43.0](https://github.com/digdir/dialogporten/compare/v1.42.1...v1.43.0) (2025-01-07)


### Features

* Add additional types to DialogActivity ([#1629](https://github.com/digdir/dialogporten/issues/1629)) ([feb1347](https://github.com/digdir/dialogporten/commit/feb1347c0a79406e0a8f6bb312faad42c8db7eec))


### Bug Fixes

* **app:** Add dedicated scope and dbcontext to GetSubjectResources ([#1648](https://github.com/digdir/dialogporten/issues/1648)) ([d1040e4](https://github.com/digdir/dialogporten/commit/d1040e41e2b09d1b8e3388ada4790ab1d63c738b))
* revert azure monitor workspace ([#1624](https://github.com/digdir/dialogporten/issues/1624)) ([d66b155](https://github.com/digdir/dialogporten/commit/d66b155f3e6749466c344ee9aa9319810f65cf6c))

## [1.42.1](https://github.com/digdir/dialogporten/compare/v1.42.0...v1.42.1) (2024-12-25)


### Bug Fixes

* **webapi:** Only allow transmissionId on TransmissionOpened activities ([#1631](https://github.com/digdir/dialogporten/issues/1631)) ([80261d1](https://github.com/digdir/dialogporten/commit/80261d18af159acd000c2fa06d6ae351aa681d7d))

## [1.42.0](https://github.com/digdir/dialogporten/compare/v1.41.3...v1.42.0) (2024-12-16)


### Features

* **apps:** add otel exporter for graphql, service and web-api ([#1528](https://github.com/digdir/dialogporten/issues/1528)) ([cb9238e](https://github.com/digdir/dialogporten/commit/cb9238ef76188b4dde371e08b7ce597645bcd8b7))

## [1.41.3](https://github.com/digdir/dialogporten/compare/v1.41.2...v1.41.3) (2024-12-13)


### Bug Fixes

* **azure:** adjust SKU and storage for staging ([#1601](https://github.com/digdir/dialogporten/issues/1601)) ([3fb9f95](https://github.com/digdir/dialogporten/commit/3fb9f9501b4db97847aa1ebc0b77efe722811f0a))
* Collapse subject resource mappings before building sql query ([#1579](https://github.com/digdir/dialogporten/issues/1579)) ([b39c376](https://github.com/digdir/dialogporten/commit/b39c37662f61361b083d7addc60b26ad4e06fab6))
* **webapi:** Explicit null on non-nullable lists no longer causes 500 INTERNAL SERVER ERROR ([#1602](https://github.com/digdir/dialogporten/issues/1602)) ([2e8b3e6](https://github.com/digdir/dialogporten/commit/2e8b3e6db507efd195245ad829dd7d5a96f272ef))

## [1.41.2](https://github.com/digdir/dialogporten/compare/v1.41.1...v1.41.2) (2024-12-12)


### Bug Fixes

* **webapi:** Set correct swagger return type for transmission list ([#1590](https://github.com/digdir/dialogporten/issues/1590)) ([6e88e0c](https://github.com/digdir/dialogporten/commit/6e88e0c13c089d0f4871be2ee95a7f74fb21a51c))

## [1.41.1](https://github.com/digdir/dialogporten/compare/v1.41.0...v1.41.1) (2024-12-09)


### Bug Fixes

* **webapi:** Return 410 GONE for sub-resources on soft-deleted dialogs ([#1564](https://github.com/digdir/dialogporten/issues/1564)) ([bb601a9](https://github.com/digdir/dialogporten/commit/bb601a99a2da2f15f3a5411fe756f8bc0df9b344))

## [1.41.0](https://github.com/digdir/dialogporten/compare/v1.40.1...v1.41.0) (2024-12-05)


### Features

* Enable FusionCache AutoClone ([#1550](https://github.com/digdir/dialogporten/issues/1550)) ([714ad5c](https://github.com/digdir/dialogporten/commit/714ad5c6498a87430408f7f485cd35d0643057c0))

## [1.40.1](https://github.com/digdir/dialogporten/compare/v1.40.0...v1.40.1) (2024-11-29)


### Bug Fixes

* **webapi:** Repeat delete requests should return 400 BAD REQUEST ([#1542](https://github.com/digdir/dialogporten/issues/1542)) ([f14861d](https://github.com/digdir/dialogporten/commit/f14861dd72e4bea41e8b8d9e2914966b1ba3f828))

## [1.40.0](https://github.com/digdir/dialogporten/compare/v1.39.0...v1.40.0) (2024-11-26)


### Features

* **infra:** Upgrade to PostgreSQL v16  ([#1521](https://github.com/digdir/dialogporten/issues/1521)) ([c67dc27](https://github.com/digdir/dialogporten/commit/c67dc27f76a6975ff411f333a71860dff6cffd54)), closes [#1520](https://github.com/digdir/dialogporten/issues/1520)


### Bug Fixes

* **app:** Sub-parties sometimes missing from authorized parties ([#1534](https://github.com/digdir/dialogporten/issues/1534)) ([f47112e](https://github.com/digdir/dialogporten/commit/f47112e1035a8b5954ecac6cf8fc75bd88620d54))
* Don't rethrow deserialization exceptions from FusionCache ([#1535](https://github.com/digdir/dialogporten/issues/1535)) ([790feb8](https://github.com/digdir/dialogporten/commit/790feb844d1d3076afcc7a7dc34590dc974f79c3))
* Use service resource org, allow admin-scope to fetch/update dialogs ([#1529](https://github.com/digdir/dialogporten/issues/1529)) ([25277b5](https://github.com/digdir/dialogporten/commit/25277b53714e8b073864cd0b2d98b512e8e0e5b6))

## [1.39.0](https://github.com/digdir/dialogporten/compare/v1.38.0...v1.39.0) (2024-11-22)


### Features

* **azure:** adjust SKU and storage for yt01 and prod ([b7e4909](https://github.com/digdir/dialogporten/commit/b7e490930261ca3470a8bb7da3715529dbe9f445))
* **azure:** adjust SKU and storage for yt01 and prod ([#1508](https://github.com/digdir/dialogporten/issues/1508)) ([5478275](https://github.com/digdir/dialogporten/commit/5478275de065ba59bca864e3808718231b3725b0))
* **graphql:** Create separate type for sub-parties ([#1510](https://github.com/digdir/dialogporten/issues/1510)) ([9c75f11](https://github.com/digdir/dialogporten/commit/9c75f113acc77afd27b08199a0b1e4bd49778e53))


### Bug Fixes

* **azure:** ensure correct properties are used when adjusting SKU and storage for postgres ([#1514](https://github.com/digdir/dialogporten/issues/1514)) ([c51d2f5](https://github.com/digdir/dialogporten/commit/c51d2f5131a6dc73e1bba61d71550e5e046cfa70))
* Reenable party list cache, log party name look failure with negative cache TTL ([#1395](https://github.com/digdir/dialogporten/issues/1395)) ([d18bb76](https://github.com/digdir/dialogporten/commit/d18bb76c07bebee46adb447f0b11f614f2851ce4))

## [1.38.0](https://github.com/digdir/dialogporten/compare/v1.37.0...v1.38.0) (2024-11-21)


### Features

* **azure:** connect cae to azure monitor ([#1486](https://github.com/digdir/dialogporten/issues/1486)) ([cf18b90](https://github.com/digdir/dialogporten/commit/cf18b90e6a3f950e6f0f7bb539e799058e136312))

## [1.37.0](https://github.com/digdir/dialogporten/compare/v1.36.0...v1.37.0) (2024-11-20)


### Features

* **performance:** Refactoring and tracing ([#1489](https://github.com/digdir/dialogporten/issues/1489)) ([760c345](https://github.com/digdir/dialogporten/commit/760c3452cb851ec2044101e229e45d79b7d5b6c6))

## [1.36.0](https://github.com/digdir/dialogporten/compare/v1.35.0...v1.36.0) (2024-11-19)


### Features

* **azure:** create azure monitor workspace ([#1485](https://github.com/digdir/dialogporten/issues/1485)) ([da0aa8f](https://github.com/digdir/dialogporten/commit/da0aa8f974742c146207e64db817bbb6e732dff2))


### Bug Fixes

* **app:** Error details missing when user type is unknown ([#1493](https://github.com/digdir/dialogporten/issues/1493)) ([9fbd2cf](https://github.com/digdir/dialogporten/commit/9fbd2cf505cbab1129c0ba75c6a609fc9e3ea44a))
* **azure:** enable public access for azure monitor ([#1496](https://github.com/digdir/dialogporten/issues/1496)) ([b0d5794](https://github.com/digdir/dialogporten/commit/b0d5794a5c31f979a85f64f512fb3cb2b000b139))
* **azure:** ensure monitor workspace is reachable ([#1494](https://github.com/digdir/dialogporten/issues/1494)) ([dc7fc1f](https://github.com/digdir/dialogporten/commit/dc7fc1f354f40c1e4dc5f9a1a0e729f1bc3d171d))
* **webapi:** Require base service provider scope on search endpoint ([#1476](https://github.com/digdir/dialogporten/issues/1476)) ([8c41f3d](https://github.com/digdir/dialogporten/commit/8c41f3d54edc3edec1f48dc1f701e4b83163535a))

## [1.35.0](https://github.com/digdir/dialogporten/compare/v1.34.0...v1.35.0) (2024-11-15)


### Features

* Synchronization of resource policy metadata ([#1411](https://github.com/digdir/dialogporten/issues/1411)) ([193b764](https://github.com/digdir/dialogporten/commit/193b7645ff45155cedc9a952e4322c5e55642cf8))

## [1.34.0](https://github.com/digdir/dialogporten/compare/v1.33.1...v1.34.0) (2024-11-14)


### Features

* **azure:** enable index tuning for postgres in YT ([#1455](https://github.com/digdir/dialogporten/issues/1455)) ([69f01ae](https://github.com/digdir/dialogporten/commit/69f01aedcff0eb28b8bff80dfd1cec709e0c4409))

## [1.33.1](https://github.com/digdir/dialogporten/compare/v1.33.0...v1.33.1) (2024-11-14)


### Bug Fixes

* **bicep:** Add missing SKU for postgres create ([#1453](https://github.com/digdir/dialogporten/issues/1453)) ([ab8cb03](https://github.com/digdir/dialogporten/commit/ab8cb03d9430bc34608637921a31bc21591a2f1c))

## [1.33.0](https://github.com/digdir/dialogporten/compare/v1.32.1...v1.33.0) (2024-11-14)


### Features

* **azure:** Upgrade postgres SKU for prod/yt01 ([#1450](https://github.com/digdir/dialogporten/issues/1450)) ([b7586f2](https://github.com/digdir/dialogporten/commit/b7586f2ea0da43b4b2819f75f7bb2a9c1dcc5ad0))

## [1.32.1](https://github.com/digdir/dialogporten/compare/v1.32.0...v1.32.1) (2024-11-13)


### Bug Fixes

* **azure:** ensure postgres configuration run in sequence ([#1448](https://github.com/digdir/dialogporten/issues/1448)) ([a5a6868](https://github.com/digdir/dialogporten/commit/a5a6868037619172a97ca1d1acde85075825adbd))

## [1.32.0](https://github.com/digdir/dialogporten/compare/v1.31.0...v1.32.0) (2024-11-12)


### Features

* **graphql:** Set max execution depth to 10 ([#1431](https://github.com/digdir/dialogporten/issues/1431)) ([8845e49](https://github.com/digdir/dialogporten/commit/8845e49cc687230d72a8eea6e65c9c210886d7ee)), closes [#1430](https://github.com/digdir/dialogporten/issues/1430)
* **performance:** create a k6 purge script to run after creating dialogs ([#1435](https://github.com/digdir/dialogporten/issues/1435)) ([9555d78](https://github.com/digdir/dialogporten/commit/9555d7861fe54ab1530b2ac4cccbb7c41e868c0b))
* **performance:** Expands search for serviceowners, improved tracing and logging ([#1439](https://github.com/digdir/dialogporten/issues/1439)) ([b1d6eaf](https://github.com/digdir/dialogporten/commit/b1d6eafa159f35659bbd4d878028e8fb364e2666))

## [1.31.0](https://github.com/digdir/dialogporten/compare/v1.30.0...v1.31.0) (2024-11-08)


### Features

* **azure:** enable query performance insights for postgres ([#1417](https://github.com/digdir/dialogporten/issues/1417)) ([bb832d8](https://github.com/digdir/dialogporten/commit/bb832d8d923114e204b448d3fbb6a23c249aad3a))


### Bug Fixes

* add timeout for health checks ([#1388](https://github.com/digdir/dialogporten/issues/1388)) ([d68cc65](https://github.com/digdir/dialogporten/commit/d68cc65d937c48859f69666a10cc7f860715ade2))
* **azure:** set diagnostic setting to allow query perf insights ([#1422](https://github.com/digdir/dialogporten/issues/1422)) ([5919258](https://github.com/digdir/dialogporten/commit/5919258284acd0b1416508839d9802480b2938b5))

## [1.30.0](https://github.com/digdir/dialogporten/compare/v1.29.0...v1.30.0) (2024-11-08)


### Features

* **performance:** Performance/create serviceowner search ([#1413](https://github.com/digdir/dialogporten/issues/1413)) ([f1096a4](https://github.com/digdir/dialogporten/commit/f1096a4eec7e7ea0b08d34bd4c9776f3c86fcd66))
* **webapi:** Combine actorDtos ([#1374](https://github.com/digdir/dialogporten/issues/1374)) ([ca18a99](https://github.com/digdir/dialogporten/commit/ca18a993f21e488bfe4be7c167c822a7954b2683))
* **webapi:** Limit Content-Length / request body size ([#1416](https://github.com/digdir/dialogporten/issues/1416)) ([44be20a](https://github.com/digdir/dialogporten/commit/44be20affccdb8f879b7118ebd69a72bef9d5f50))

## [1.29.0](https://github.com/digdir/dialogporten/compare/v1.28.3...v1.29.0) (2024-11-06)


### Features

* **webAPI:** Make all lists nullable in OpenAPI schema ([#1359](https://github.com/digdir/dialogporten/issues/1359)) ([920d749](https://github.com/digdir/dialogporten/commit/920d7493d09e551a4207f61636a7188fea490223))


### Bug Fixes

* **graphql:** ensure gql has maskinporten environment set ([#1408](https://github.com/digdir/dialogporten/issues/1408)) ([152417a](https://github.com/digdir/dialogporten/commit/152417aa100bb779e68d302c0674e2f9ed2b649e))

## [1.28.3](https://github.com/digdir/dialogporten/compare/v1.28.2...v1.28.3) (2024-11-06)


### Bug Fixes

* avoid crash if testdata file is empty ([#1403](https://github.com/digdir/dialogporten/issues/1403)) ([e0ea0af](https://github.com/digdir/dialogporten/commit/e0ea0afad3a62cf67b495c68405eb420586f80a3))

## [1.28.2](https://github.com/digdir/dialogporten/compare/v1.28.1...v1.28.2) (2024-11-05)


### Bug Fixes

* Use yt01 token generator environment for k6 tests running on yt01 ([#1391](https://github.com/digdir/dialogporten/issues/1391)) ([393176c](https://github.com/digdir/dialogporten/commit/393176c1f21dc6f8b0ab7fbf294e16713bd4d6e0))

## [1.28.1](https://github.com/digdir/dialogporten/compare/v1.28.0...v1.28.1) (2024-11-05)


### Bug Fixes

* **service:** ensure correct maskinporten environment ([#1392](https://github.com/digdir/dialogporten/issues/1392)) ([9d7defe](https://github.com/digdir/dialogporten/commit/9d7defe02b2dd97c87056faf93c846fb8e3ab320))

## [1.28.0](https://github.com/digdir/dialogporten/compare/v1.27.1...v1.28.0) (2024-11-05)


### Features

* update swagger name generation ([#1350](https://github.com/digdir/dialogporten/issues/1350)) ([94c5544](https://github.com/digdir/dialogporten/commit/94c55446dbc52ec69def8a74bab6bf7a928d2f3c))
* **webapi:** Add ExternalReference to dialog search result ([#1384](https://github.com/digdir/dialogporten/issues/1384)) ([431fe16](https://github.com/digdir/dialogporten/commit/431fe16587c787e785a8a100f3c464c339d5ee0b))
* **webapi:** Return 410 GONE for notification checks on deleted dialogs ([#1387](https://github.com/digdir/dialogporten/issues/1387)) ([198bebd](https://github.com/digdir/dialogporten/commit/198bebd6d44554b4c66917c9d6921e730ab648fe))


### Bug Fixes

* Add system user id to identifying claims ([#1362](https://github.com/digdir/dialogporten/issues/1362)) ([16f160d](https://github.com/digdir/dialogporten/commit/16f160d5f5a2293444ac63c0ae13a713b3afe318))
* **e2e:** Use pagination in sentinel ([#1372](https://github.com/digdir/dialogporten/issues/1372)) ([a1df0ff](https://github.com/digdir/dialogporten/commit/a1df0ff06bbc10c07db100d35fafa85c3f95393d))
* fixed placement of referenced workflow-file ([#1365](https://github.com/digdir/dialogporten/issues/1365)) ([49c1d80](https://github.com/digdir/dialogporten/commit/49c1d8042040fe5e9eef1646a76b7c7ecaac062f))
* workaround for github number error in dispatch workflow ([#1367](https://github.com/digdir/dialogporten/issues/1367)) ([06ee356](https://github.com/digdir/dialogporten/commit/06ee3563efcd37156aea755db03c90666610e625))

## [1.27.1](https://github.com/digdir/dialogporten/compare/v1.27.0...v1.27.1) (2024-10-30)


### Bug Fixes

* Simplify subject attribute matching ([#1348](https://github.com/digdir/dialogporten/issues/1348)) ([55159b7](https://github.com/digdir/dialogporten/commit/55159b772578e58d3406dd8028e9c14d9b3254e1))

## [1.27.0](https://github.com/digdir/dialogporten/compare/v1.26.3...v1.27.0) (2024-10-29)


### Features

* Add restrictions to Transmissions reference hierarchy ([#1310](https://github.com/digdir/dialogporten/issues/1310)) ([e3d53ca](https://github.com/digdir/dialogporten/commit/e3d53cafbbb7157d8439c23745d6b23cbbaeea17))
* **graphql:** configure opentelemetry ([#1343](https://github.com/digdir/dialogporten/issues/1343)) ([e31c08b](https://github.com/digdir/dialogporten/commit/e31c08b0ddcad8b43db2c1ce7f46be5b924fdb9d))
* **infrastructure:** add availability test for apim ([#1327](https://github.com/digdir/dialogporten/issues/1327)) ([1f9fa2b](https://github.com/digdir/dialogporten/commit/1f9fa2b3fbb7ea9bd84ddde5f99697724785921d))
* **service:** configure opentelemetry ([#1342](https://github.com/digdir/dialogporten/issues/1342)) ([513d5e4](https://github.com/digdir/dialogporten/commit/513d5e4bf3345ecf70c5adb858143025db2738fa))
* **utils:** configure open telemetry tracing for masstransit in aspnet package ([#1344](https://github.com/digdir/dialogporten/issues/1344)) ([5ec3b84](https://github.com/digdir/dialogporten/commit/5ec3b84be6955963cda92ab209510ad01d4dda90))

## [1.26.3](https://github.com/digdir/dialogporten/compare/v1.26.2...v1.26.3) (2024-10-23)


### Bug Fixes

* Fix XACML attribute id for system users ([#1340](https://github.com/digdir/dialogporten/issues/1340)) ([4257729](https://github.com/digdir/dialogporten/commit/42577295a78426132eafeaeaa536e88f711e50bc))
* **service:** enable health-check for servicebus ([#1338](https://github.com/digdir/dialogporten/issues/1338)) ([480f5e3](https://github.com/digdir/dialogporten/commit/480f5e37e299c032fe06d1071872c599fdc1dcfc))

## [1.26.2](https://github.com/digdir/dialogporten/compare/v1.26.1...v1.26.2) (2024-10-23)


### Bug Fixes

* **slack-notifier:** exclude health checks from alerts ([#1335](https://github.com/digdir/dialogporten/issues/1335)) ([0a4331a](https://github.com/digdir/dialogporten/commit/0a4331a7508bc59353b539fde27412f17d6e7de8))

## [1.26.1](https://github.com/digdir/dialogporten/compare/v1.26.0...v1.26.1) (2024-10-22)


### Bug Fixes

* **service:** add appsettings for the yt01 environment ([#1329](https://github.com/digdir/dialogporten/issues/1329)) ([ef2981b](https://github.com/digdir/dialogporten/commit/ef2981b50d9eb9786c6efd19305f3a69e9ce2bf0))

## [1.26.0](https://github.com/digdir/dialogporten/compare/v1.25.0...v1.26.0) (2024-10-22)


### Features

* Add masstransit outbox system ([#1277](https://github.com/digdir/dialogporten/issues/1277)) ([bc04860](https://github.com/digdir/dialogporten/commit/bc048604e96bac67c91193c7d82b031bd9be2923))


### Bug Fixes

* **infrastructure:** use correct networking for servicebus ([#1320](https://github.com/digdir/dialogporten/issues/1320)) ([4fb42bb](https://github.com/digdir/dialogporten/commit/4fb42bbe0af6a9023369f0676be6f38e9fd7c780))
* Return distinct actions in GetAlinnActions ([#1298](https://github.com/digdir/dialogporten/issues/1298)) ([49948b2](https://github.com/digdir/dialogporten/commit/49948b246247d7798496ddb0225620c809aee4f1))
* Upgraded Altinn.ApiClients.Maskinporten, specify TokenExchangeEnvironment ([#1328](https://github.com/digdir/dialogporten/issues/1328)) ([5156799](https://github.com/digdir/dialogporten/commit/51567996b11ceb76b503b22aa0226acd575aaad2))

## [1.25.0](https://github.com/digdir/dialogporten/compare/v1.24.0...v1.25.0) (2024-10-17)


### Features

* **applications:** add scalers for cpu and memory ([#1295](https://github.com/digdir/dialogporten/issues/1295)) ([eb0f19b](https://github.com/digdir/dialogporten/commit/eb0f19bfb5a49da1b4b45a15b6e43785212fc62f))
* **infrastructure:** create new yt01 app environment ([#1291](https://github.com/digdir/dialogporten/issues/1291)) ([1a1ccc0](https://github.com/digdir/dialogporten/commit/1a1ccc0a81da0be7bf89b105dc3af57ee8ae4e93))
* **service:** add permissions for service-bus ([#1305](https://github.com/digdir/dialogporten/issues/1305)) ([7bf4177](https://github.com/digdir/dialogporten/commit/7bf41775fa2e1c343972df75d3e4138647fa5742))
* **service:** deploy application in container apps ([#1303](https://github.com/digdir/dialogporten/issues/1303)) ([a309044](https://github.com/digdir/dialogporten/commit/a309044bd40d9a56c453496aab9122b8f6c67adb))


### Bug Fixes

* **applications:** add missing property for scale configuration ([3ffb724](https://github.com/digdir/dialogporten/commit/3ffb72476e1085347f51e39e25600bc7a4de69ea))
* **applications:** use correct scale configuration ([#1311](https://github.com/digdir/dialogporten/issues/1311)) ([b8fb3cc](https://github.com/digdir/dialogporten/commit/b8fb3cc956b5365b4008abc946e4d967fd710efe))
* Fix ID-porten acr claim parsing ([#1299](https://github.com/digdir/dialogporten/issues/1299)) ([8b8862f](https://github.com/digdir/dialogporten/commit/8b8862fb781a9c57dcd9f3c8315ce66c64d399e2))
* **service:** ensure default credentials work ([#1306](https://github.com/digdir/dialogporten/issues/1306)) ([b1e6a14](https://github.com/digdir/dialogporten/commit/b1e6a1495e6ca9cd25a6a8cf060f39456db95c30))

## [1.24.0](https://github.com/digdir/dialogporten/compare/v1.23.2...v1.24.0) (2024-10-15)


### Features

* **infrastructure:** create new yt01 infrastructure environment ([#1290](https://github.com/digdir/dialogporten/issues/1290)) ([2044070](https://github.com/digdir/dialogporten/commit/2044070e981a7c3bc3182f1659342fb9585fd67d))


### Bug Fixes

* Fallback to using list auth if details auth fails, remove double cache ([#1274](https://github.com/digdir/dialogporten/issues/1274)) ([54425e7](https://github.com/digdir/dialogporten/commit/54425e76ecaf3d8cedd06aaa30506b59de019da3))

## [1.23.2](https://github.com/digdir/dialogporten/compare/v1.23.1...v1.23.2) (2024-10-14)


### Bug Fixes

* **webAPI:** Allow front channel embeds on TransmissionContent ([#1276](https://github.com/digdir/dialogporten/issues/1276)) ([c87e8f4](https://github.com/digdir/dialogporten/commit/c87e8f4a880e1ed12bda2848e7b745c77cc0c6fa))

## [1.23.1](https://github.com/digdir/dialogporten/compare/v1.23.0...v1.23.1) (2024-10-11)


### Bug Fixes

* **graphql:** refactor health check probes ([#1250](https://github.com/digdir/dialogporten/issues/1250)) ([1e9c350](https://github.com/digdir/dialogporten/commit/1e9c3505c004efe1b2e1c0cfe1e6c2a146a4af55))

## [1.23.0](https://github.com/digdir/dialogporten/compare/v1.22.0...v1.23.0) (2024-10-10)


### Features

* **infra:** upgrade postgresql SKU in test ([#1257](https://github.com/digdir/dialogporten/issues/1257)) ([5a751af](https://github.com/digdir/dialogporten/commit/5a751af66253515e91bb5d13f2eaefbee8313cf4))
* **webAPI:** Add legacy HTML support for MainContentReference ([#1256](https://github.com/digdir/dialogporten/issues/1256)) ([482b38a](https://github.com/digdir/dialogporten/commit/482b38a769f1cfff22dbc85ec96f6ad2bb58089f))


### Bug Fixes

* Add missing return types for Transmissions and Activities in OpenAPI spec ([#1244](https://github.com/digdir/dialogporten/issues/1244)) ([972870d](https://github.com/digdir/dialogporten/commit/972870d53b9752ecd391b07773e72ea6d08b2082))
* **graphQL:** Missing MediaType on dialog attachment url ([#1264](https://github.com/digdir/dialogporten/issues/1264)) ([3919343](https://github.com/digdir/dialogporten/commit/391934362bce6a14f6abc8bd16f66879dab30d41))
* Refactor probes and add more health checks ([#1159](https://github.com/digdir/dialogporten/issues/1159)) ([6889a96](https://github.com/digdir/dialogporten/commit/6889a96adcf7ffd141df0b854ca683e228b1a6fe))
* **webapi:** ensure correct health checks are used in probes ([#1249](https://github.com/digdir/dialogporten/issues/1249)) ([f951152](https://github.com/digdir/dialogporten/commit/f9511528804f1560992843cde9515811de9eca0a))

## [1.22.0](https://github.com/digdir/dialogporten/compare/v1.21.0...v1.22.0) (2024-10-07)


### Features

* Add support for supplied transmission attachment ID on create/update ([#1242](https://github.com/digdir/dialogporten/issues/1242)) ([c7bfb07](https://github.com/digdir/dialogporten/commit/c7bfb076fd8c8e0c853d3b99c346a87a89501170))


### Bug Fixes

* Only allow legacy HTML on AditionalInfo content ([#1210](https://github.com/digdir/dialogporten/issues/1210)) ([aa4acde](https://github.com/digdir/dialogporten/commit/aa4acde212e76cb3665fee0daaf116d9837c4fc9))
* **webAPI:** Specifying EndUserId on the ServiceOwner Search endpoint produces 500 - Internal Server error ([#1234](https://github.com/digdir/dialogporten/issues/1234)) ([49c0d34](https://github.com/digdir/dialogporten/commit/49c0d3438c396e2ca82a6101bd37e402a0c3aec9))

## [1.21.0](https://github.com/digdir/dialogporten/compare/v1.20.2...v1.21.0) (2024-10-03)


### Features

* basic label implementation to hide dialogs ([#1192](https://github.com/digdir/dialogporten/issues/1192)) ([ee90c68](https://github.com/digdir/dialogporten/commit/ee90c6806bf0b394d9062612f5554a4d02616ab4))


### Bug Fixes

* **webAPI:** Broken mapping when creating Transmissions in an Update ([#1221](https://github.com/digdir/dialogporten/issues/1221)) ([6e7dfe4](https://github.com/digdir/dialogporten/commit/6e7dfe461eb1841bbdd1dd721fab87e7b609756c))

## [1.20.2](https://github.com/digdir/dialogporten/compare/v1.20.1...v1.20.2) (2024-10-02)


### Bug Fixes

* (webAPI): Add revision to search dto (ServiceOwner) ([#1216](https://github.com/digdir/dialogporten/issues/1216)) ([3b6d130](https://github.com/digdir/dialogporten/commit/3b6d130bb117fa8d3e0a183474c9bd60e377abb7))
* **graphQL:** GraphQL subscription not notified on DialogActivityCreated ([#1187](https://github.com/digdir/dialogporten/issues/1187)) ([f28e291](https://github.com/digdir/dialogporten/commit/f28e291bdba7cf3cc94cf0de84fcc12e781d3abb))

## [1.20.1](https://github.com/digdir/dialogporten/compare/v1.20.0...v1.20.1) (2024-10-02)


### Bug Fixes

* Add separate settings for parties cache, don't cache invalid response from Altinn 2 ([#1194](https://github.com/digdir/dialogporten/issues/1194)) ([dbb79dc](https://github.com/digdir/dialogporten/commit/dbb79dc26cefc5f28c21a738f39199c36a49438f))

## [1.20.0](https://github.com/digdir/dialogporten/compare/v1.19.0...v1.20.0) (2024-09-30)


### Features

* **GraphQL:** Add DialogToken requirement for subscriptions ([#1124](https://github.com/digdir/dialogporten/issues/1124)) ([651ca62](https://github.com/digdir/dialogporten/commit/651ca62fdec02dec48b674b80acf52737036cf13))

## [1.19.0](https://github.com/digdir/dialogporten/compare/v1.18.1...v1.19.0) (2024-09-24)


### Features

* **breaking:** Move notification check endpoint to /actions ([#1175](https://github.com/digdir/dialogporten/issues/1175)) ([e0c1cf2](https://github.com/digdir/dialogporten/commit/e0c1cf205c66200f024431dc3392c988b99fdb30))


### Bug Fixes

* **janitor:** ensure Redis is configured correctly ([#1182](https://github.com/digdir/dialogporten/issues/1182)) ([37fe982](https://github.com/digdir/dialogporten/commit/37fe982ea9f08e48c75481008d614aaacf19a57d))

## [1.18.1](https://github.com/digdir/dialogporten/compare/v1.18.0...v1.18.1) (2024-09-23)


### Bug Fixes

* Add missing events to dialog subscription ([#1163](https://github.com/digdir/dialogporten/issues/1163)) ([162ce9a](https://github.com/digdir/dialogporten/commit/162ce9a9a0c4183d10e8edfe0f8c5589110b7a59))
* Fix BaseUri on localhost trailing slash discrepancy on OAuth metadata ([#1145](https://github.com/digdir/dialogporten/issues/1145)) ([09ce878](https://github.com/digdir/dialogporten/commit/09ce878cb537bf2e495e3801e0c769e25008246a))

## [1.18.0](https://github.com/digdir/dialogporten/compare/v1.17.0...v1.18.0) (2024-09-16)


### Features

* add dialogOpened activitytype ([#1110](https://github.com/digdir/dialogporten/issues/1110)) ([711fa6d](https://github.com/digdir/dialogporten/commit/711fa6dcbd3e8ab1240c765b9fe1b765f00fe86d))
* Add process and precedingProcess to dialog as optional fields ([#1092](https://github.com/digdir/dialogporten/issues/1092)) ([2bf0d30](https://github.com/digdir/dialogporten/commit/2bf0d30619f6c40716a70890cda47fa7b30ad0ac))


### Bug Fixes

* Allow setting UpdatedAt when creating Dialog ([#1105](https://github.com/digdir/dialogporten/issues/1105)) ([481e907](https://github.com/digdir/dialogporten/commit/481e907993a1c32337ea6a85ced8312ec4cd1e5b))
* Authorize access to dialog details for any mainresource action ([#1122](https://github.com/digdir/dialogporten/issues/1122)) ([a7e769a](https://github.com/digdir/dialogporten/commit/a7e769ad2be45c7f72169f7ae980ab24fd43ce72))

## [1.17.0](https://github.com/digdir/dialogporten/compare/v1.16.0...v1.17.0) (2024-09-10)


### Features

* Add SubjectResource entity and db migration ([#1048](https://github.com/digdir/dialogporten/issues/1048)) ([d04d764](https://github.com/digdir/dialogporten/commit/d04d764b80b855b4d906d23d48de53720b2d8bf1))
* **graphQL:** Add subscription for dialog details ([#1072](https://github.com/digdir/dialogporten/issues/1072)) ([8214acb](https://github.com/digdir/dialogporten/commit/8214acbf61085cacadaea3ef7e5f3d6ac222cc2c))
* Implement scalable dialog search authorization ([#875](https://github.com/digdir/dialogporten/issues/875)) ([aa8f84d](https://github.com/digdir/dialogporten/commit/aa8f84ded3aaf569e97e2d85f1035d1b14c59915))
* revise dialog status ([#1099](https://github.com/digdir/dialogporten/issues/1099)) ([0029f46](https://github.com/digdir/dialogporten/commit/0029f46c464e4ff05443cebb73be13b52879ab93))


### Bug Fixes

* ensure correct appsettings is used ([#1086](https://github.com/digdir/dialogporten/issues/1086)) ([d43f6d7](https://github.com/digdir/dialogporten/commit/d43f6d7d04108c8baaf131feb0b4a9c4efd18a42))
* ensure jobs are run with correct arguments and parameters ([#1085](https://github.com/digdir/dialogporten/issues/1085)) ([e21de56](https://github.com/digdir/dialogporten/commit/e21de56130c48684324fd648699a5965c8a88ebf))
* **webapi:** Return 422 when existing transmission IDs are used in dialog update ([#1094](https://github.com/digdir/dialogporten/issues/1094)) ([7a8a933](https://github.com/digdir/dialogporten/commit/7a8a933fd63f7c456f625bd1885ad429d1fc0832))

## [1.16.0](https://github.com/digdir/dialogporten/compare/v1.15.0...v1.16.0) (2024-09-04)


### Features

* **webapi:** Require legacy scope for HTML support ([#1073](https://github.com/digdir/dialogporten/issues/1073)) ([03237cc](https://github.com/digdir/dialogporten/commit/03237cc29d05f34dce3f683368117f546de40762))
* **webAPI:** Require UUIDv7  ([#1032](https://github.com/digdir/dialogporten/issues/1032)) ([e9b844f](https://github.com/digdir/dialogporten/commit/e9b844f8092bbb28c0ec1d63676593d78719954b))


### Bug Fixes

* Fix iss claim in dialog token ([#1047](https://github.com/digdir/dialogporten/issues/1047)) ([9ab4a85](https://github.com/digdir/dialogporten/commit/9ab4a85eea321fd80215616580785f4c99fa85bb))

## [1.15.0](https://github.com/digdir/dialogporten/compare/v1.14.0...v1.15.0) (2024-08-21)


### Features

* add support for serviceowner admin scope ([#1002](https://github.com/digdir/dialogporten/issues/1002)) ([2638b48](https://github.com/digdir/dialogporten/commit/2638b485f50ec7973aaf2fbdfb02ab07cb913f99))
* **web-api:** Add optional EndUserId param to ServiceOwner Get Dialog details API ([#1020](https://github.com/digdir/dialogporten/issues/1020)) ([1380b33](https://github.com/digdir/dialogporten/commit/1380b33f4b80cb25146a9785a174091a2db8465a))


### Bug Fixes

* **azure:** use correct ip for APIM in prod ([#1036](https://github.com/digdir/dialogporten/issues/1036)) ([fecc4c0](https://github.com/digdir/dialogporten/commit/fecc4c0b38d8c97413b10f5201749b9817ad6e31))

## [1.14.0](https://github.com/digdir/dialogporten/compare/v1.13.0...v1.14.0) (2024-08-19)


### Features

* **web-api:** add production config ([#1018](https://github.com/digdir/dialogporten/issues/1018)) ([689e7fe](https://github.com/digdir/dialogporten/commit/689e7fe2b087aac5609efddf146cf3b7f280a6fa))

## [1.13.0](https://github.com/digdir/dialogporten/compare/v1.12.1...v1.13.0) (2024-08-15)


### Features

* Add doc blocks on DTOs for OAS generation, CORS headers ([#987](https://github.com/digdir/dialogporten/issues/987)) ([01c34b8](https://github.com/digdir/dialogporten/commit/01c34b841c82b58fe96e0c0831c2dcb49902804e))
* **azure:** add bicep parameter files for production ([#1016](https://github.com/digdir/dialogporten/issues/1016)) ([7a7198a](https://github.com/digdir/dialogporten/commit/7a7198a6579ab2143a0a0250039f3ebbac6bf7b3))

## [1.12.1](https://github.com/digdir/dialogporten/compare/v1.12.0...v1.12.1) (2024-08-14)


### Bug Fixes

* **azure:** ensure environment parameter for production is correct ([#1014](https://github.com/digdir/dialogporten/issues/1014)) ([1612f9d](https://github.com/digdir/dialogporten/commit/1612f9dab192831b5214a8f2a3180b10100e24f5))

## [1.12.0](https://github.com/digdir/dialogporten/compare/v1.11.0...v1.12.0) (2024-08-14)


### Features

* Add current user flag to parties dto ([#993](https://github.com/digdir/dialogporten/issues/993)) ([e096743](https://github.com/digdir/dialogporten/commit/e0967436cea9f1efa8dca503e511dc66cf830591))
* Add notification condition check endpoint ([#965](https://github.com/digdir/dialogporten/issues/965)) ([f480ce0](https://github.com/digdir/dialogporten/commit/f480ce0733453864ce3bb2aa28d1fb4bba2655d2))


### Bug Fixes

* Using existing Transmission or Activity IDs should no longer result in internal server error on updates ([#980](https://github.com/digdir/dialogporten/issues/980)) ([0757b33](https://github.com/digdir/dialogporten/commit/0757b332e1194aee399b6a0ab7c6c66d5fbf037e))

## [1.11.0](https://github.com/digdir/dialogporten/compare/v1.10.0...v1.11.0) (2024-08-08)


### Features

* **azure:** scaffold ssh jumper ([#958](https://github.com/digdir/dialogporten/issues/958)) ([6228aa2](https://github.com/digdir/dialogporten/commit/6228aa2e543bb319ae8d8d0d097b19717b526896))


### Bug Fixes

* Correct the SeenLog list endpoints OpenAPI description ([#976](https://github.com/digdir/dialogporten/issues/976)) ([f6ebd19](https://github.com/digdir/dialogporten/commit/f6ebd19ee8ab790b3a7892776fc9e0be01004121))
* Using existing transmission/activity IDs should return HTTP 422 ([#960](https://github.com/digdir/dialogporten/issues/960)) ([01789b1](https://github.com/digdir/dialogporten/commit/01789b1f256b17445379194f3cf781a0d70fc1af)), closes [#959](https://github.com/digdir/dialogporten/issues/959)

## [1.10.0](https://github.com/digdir/dialogporten/compare/v1.9.0...v1.10.0) (2024-08-01)


### Features

* **azure:** add tags on azure applications ([#957](https://github.com/digdir/dialogporten/issues/957)) ([4081922](https://github.com/digdir/dialogporten/commit/4081922c47aa83fc95b6583f1d50ebf941d7e020))


### Bug Fixes

* **azure:** add product tag on all resources ([#955](https://github.com/digdir/dialogporten/issues/955)) ([6c76576](https://github.com/digdir/dialogporten/commit/6c76576397424e12feafa694b69a1a2e8bbd4d1f))

## [1.9.0](https://github.com/digdir/dialogporten/compare/v1.8.1...v1.9.0) (2024-07-30)


### Features

* **breaking:** Move front channel embeds to content ([#862](https://github.com/digdir/dialogporten/issues/862)) ([c9b50e9](https://github.com/digdir/dialogporten/commit/c9b50e9ea7022c5bf22b472cf8859fd6faf66df6))
* **breaking:** Remove DialogElements, add Attachments ([#867](https://github.com/digdir/dialogporten/issues/867)) ([dbe296a](https://github.com/digdir/dialogporten/commit/dbe296aa3f25e88227109ca604efd81616f2b4ab))
* **breaking:** Remove PUT/DELETE endpoints for DialogElements ([#844](https://github.com/digdir/dialogporten/issues/844)) ([51eb898](https://github.com/digdir/dialogporten/commit/51eb89832f56081aa4b3eb2d30b7d19b1fb8f217))
* **breaking:** Rename CultureCode to LanguageCode ([#871](https://github.com/digdir/dialogporten/issues/871)) ([96d50fc](https://github.com/digdir/dialogporten/commit/96d50fc40b075b31d342b8dc27e82924c30e9b83))
* **breaking:** Renaming dialog activity types ([#919](https://github.com/digdir/dialogporten/issues/919)) ([af262b1](https://github.com/digdir/dialogporten/commit/af262b146ce78cd7eed6325d0a3ac8d662000107))
* Change content array to object with properties for each content type ([#905](https://github.com/digdir/dialogporten/issues/905)) ([d549f19](https://github.com/digdir/dialogporten/commit/d549f194903d5a48e0563c1ceb942db2d333dd59))
* Implement actor entity ([#912](https://github.com/digdir/dialogporten/issues/912)) ([a635fcb](https://github.com/digdir/dialogporten/commit/a635fcb04e988c416ae98928af05934ddd187de1))
* Introduce Transmissions ([#932](https://github.com/digdir/dialogporten/issues/932)) ([3ca495f](https://github.com/digdir/dialogporten/commit/3ca495f0c900862c1a0bbf4b7acd8350f7649347))
* Rename DialogStatus enum values ([#915](https://github.com/digdir/dialogporten/issues/915)) ([5aea32b](https://github.com/digdir/dialogporten/commit/5aea32b7299bd459fa529ada197fe42817d3aed7))
* **WebAPI:** Add Transmission endpoints  ([#943](https://github.com/digdir/dialogporten/issues/943)) ([d608ade](https://github.com/digdir/dialogporten/commit/d608adebadc12d099c04c4b174569392837603e6))


### Bug Fixes

* Allow new activities to reference old activities ([#935](https://github.com/digdir/dialogporten/issues/935)) ([bbc443e](https://github.com/digdir/dialogporten/commit/bbc443e121ee5121bac6b9fe92ee3296bb45218f))
* **auth:** Malformed JWTs no longer results in InternalServerError  ([#870](https://github.com/digdir/dialogporten/issues/870)) ([5f2f386](https://github.com/digdir/dialogporten/commit/5f2f386dabfa3b25cc56370f7e99a02fa566d5e3))
* **slackNotifier:** Add missing deployment of Slack notifier function in staging environment  ([#861](https://github.com/digdir/dialogporten/issues/861)) ([59091f7](https://github.com/digdir/dialogporten/commit/59091f790c52fc7cae9b66f98b71c6db8e4bd9d3))
* Update e2e tests for actor model ([#918](https://github.com/digdir/dialogporten/issues/918)) ([ec1fcb1](https://github.com/digdir/dialogporten/commit/ec1fcb1c094b16a5a13b90350b7e2a58feaf9b82))
* **WebAPI:** Allow purging of softly deleted dialogs ([#940](https://github.com/digdir/dialogporten/issues/940)) ([c527c9f](https://github.com/digdir/dialogporten/commit/c527c9f0db7a5ad2136302de69aaec55b860fcc6))

## [1.8.1](https://github.com/digdir/dialogporten/compare/v1.8.0...v1.8.1) (2024-06-12)


### Bug Fixes

* **azure:** fix redis deployment ([#847](https://github.com/digdir/dialogporten/issues/847)) ([23781e6](https://github.com/digdir/dialogporten/commit/23781e65e5bd567f84e728ed3f808d10b2c904c5))

## [1.8.0](https://github.com/digdir/dialogporten/compare/v1.7.1...v1.8.0) (2024-06-12)


### Features

* Add support for external resource references in authorizationAttributes ([#801](https://github.com/digdir/dialogporten/issues/801)) ([1e674bd](https://github.com/digdir/dialogporten/commit/1e674bdd3a133fb73d9a5418822486d1c26d32de))
* Add user types ([#768](https://github.com/digdir/dialogporten/issues/768)) ([b6fd439](https://github.com/digdir/dialogporten/commit/b6fd439e8865e3eeec9470172abe7117ed948ee4))
* Front channel embeds ([#792](https://github.com/digdir/dialogporten/issues/792)) ([c3000bd](https://github.com/digdir/dialogporten/commit/c3000bdbc546670e1001d608c0f7541e7af64187))
* GUI actions without navigation ([#785](https://github.com/digdir/dialogporten/issues/785)) ([f2d9136](https://github.com/digdir/dialogporten/commit/f2d91364f708139be3c23b3be26ef95092675824))
* Remove IsBackChannel concept from GUI Actions ([#819](https://github.com/digdir/dialogporten/issues/819)) ([18101c1](https://github.com/digdir/dialogporten/commit/18101c1efa6854e3720d6335490b2142933400f3))
* Rename IsDeleteAction to IsDeleteDialogAction ([#820](https://github.com/digdir/dialogporten/issues/820)) ([18a1f6e](https://github.com/digdir/dialogporten/commit/18a1f6e2f3ac3d4d322e269a58780fa922a9f400))
* **schema:** Rename MimeType to MediaType ([#813](https://github.com/digdir/dialogporten/issues/813)) ([6490625](https://github.com/digdir/dialogporten/commit/64906258a9880899d086b16406c0a8ae85ffd073))
* **schema:** undo setting performed by if not set ([#802](https://github.com/digdir/dialogporten/issues/802)) ([c19f47a](https://github.com/digdir/dialogporten/commit/c19f47a4d1a018e7ba2ad1802ff62bc7b27f7b11))


### Bug Fixes

* remove maskinporten aux from config ([#827](https://github.com/digdir/dialogporten/issues/827)) ([2e4e81a](https://github.com/digdir/dialogporten/commit/2e4e81a2984dc23d91470d959804eb617dd63f1a))
* **schema:** add package-lock file ([#804](https://github.com/digdir/dialogporten/issues/804)) ([987dfa1](https://github.com/digdir/dialogporten/commit/987dfa170f38caf6488979e883578f43319b6cb9))

## [1.7.1](https://github.com/digdir/dialogporten/compare/v1.7.0...v1.7.1) (2024-05-31)


### Bug Fixes

* **ci:** separate migration-job deployments ([#795](https://github.com/digdir/dialogporten/issues/795)) ([c7f5dba](https://github.com/digdir/dialogporten/commit/c7f5dba8842ed373b8a7b9a8f6f75c02fc3a3f2c))

## [1.7.0](https://github.com/digdir/dialogporten/compare/v1.6.2...v1.7.0) (2024-05-30)


### Features

* Change party identifier separator to single colon ([#746](https://github.com/digdir/dialogporten/issues/746)) ([3342703](https://github.com/digdir/dialogporten/commit/3342703cbbfda501c79389f09ce2b5b8aeb19ae9))
* Correspondence dialog type  ([#692](https://github.com/digdir/dialogporten/issues/692)) ([317a213](https://github.com/digdir/dialogporten/commit/317a2137298f0034e69c2a43828c70f270c958c7))


### Bug Fixes

* Fix broken source URL in cloud events ([#753](https://github.com/digdir/dialogporten/issues/753)) ([4a45eda](https://github.com/digdir/dialogporten/commit/4a45eda8718a2a2258b61eaf768dda88a732a647))
* **graphql:** Add missing enum value ExtendedStatus in schema ([#733](https://github.com/digdir/dialogporten/issues/733)) ([8670595](https://github.com/digdir/dialogporten/commit/8670595bbe5a5eb8c0512cac6f92628bb0aac594))
* **graphql:** Make OrderBy nullable ([#741](https://github.com/digdir/dialogporten/issues/741)) ([3ae72ce](https://github.com/digdir/dialogporten/commit/3ae72cebab5efd041defdfabedaf70a9090d80b8))
* Update to new scope ([#750](https://github.com/digdir/dialogporten/issues/750)) ([d6fb439](https://github.com/digdir/dialogporten/commit/d6fb4398d56eb454195188a5f2fa64736e689567))
* **webapi:** Fix Swagger URL for new APIM ([#755](https://github.com/digdir/dialogporten/issues/755)) ([2388d54](https://github.com/digdir/dialogporten/commit/2388d5491f7ae1f9166e3ca84b07cf47e887dadd))

## [1.6.2](https://github.com/digdir/dialogporten/compare/v1.6.1...v1.6.2) (2024-05-10)


### Bug Fixes

* **gql:** Add missing graphQl appsettings for staging ([#714](https://github.com/digdir/dialogporten/issues/714)) ([97b7da6](https://github.com/digdir/dialogporten/commit/97b7da6e1b817c492467cec587513a2b4a00e518))
* Use correct scope for authorization API for remaining runtimes ([#711](https://github.com/digdir/dialogporten/issues/711)) ([0691f36](https://github.com/digdir/dialogporten/commit/0691f360b6f28cc353279b9e65c4de7e53d113e4))

## [1.6.1](https://github.com/digdir/dialogporten/compare/v1.6.0...v1.6.1) (2024-05-08)


### Bug Fixes

* Use correct scope for authorization API ([#709](https://github.com/digdir/dialogporten/issues/709)) ([38253ad](https://github.com/digdir/dialogporten/commit/38253ad1128745f54ef5d2bd4393c2d1efdae58e))

## [1.6.0](https://github.com/digdir/dialogporten/compare/v1.5.0...v1.6.0) (2024-05-07)


### Features

* Add authorization caching ([#591](https://github.com/digdir/dialogporten/issues/591)) ([2f86d7e](https://github.com/digdir/dialogporten/commit/2f86d7e6982f2d1e228a753a0b625904caf443ea))
* Add GraphQL POC ([#636](https://github.com/digdir/dialogporten/issues/636)) ([c779eac](https://github.com/digdir/dialogporten/commit/c779eac72372e925f8bb3d348812322ebdef319a))
* Add support for apps as serviceresource ([#658](https://github.com/digdir/dialogporten/issues/658)) ([adf91ce](https://github.com/digdir/dialogporten/commit/adf91ce739980abd0177b26d55f9db956f1e95fb))
* Authorized parties endpoint in enduser API ([#661](https://github.com/digdir/dialogporten/issues/661)) ([050ccbb](https://github.com/digdir/dialogporten/commit/050ccbb548d9f8bf03842041bd675f8f27504141))


### Bug Fixes

* Accept app references with urn:altinn:resource prefix ([#685](https://github.com/digdir/dialogporten/issues/685)) ([c9a5606](https://github.com/digdir/dialogporten/commit/c9a5606f932fb029ecf8a891b9a9246dc631db52))
* ensure performed by is set for activities ([#628](https://github.com/digdir/dialogporten/issues/628)) ([1adf075](https://github.com/digdir/dialogporten/commit/1adf075de2a25fa83b4909cf9b29020b53cf9c0f))
* Use HttpClient wrappers that ensure success to match FusionCache expectations ([#684](https://github.com/digdir/dialogporten/issues/684)) ([7c1e966](https://github.com/digdir/dialogporten/commit/7c1e96650cf54594a33fb380c8b42518392f1741))

## [1.5.0](https://github.com/digdir/dialogporten/compare/v1.4.0...v1.5.0) (2024-04-10)


### Features

* **azure:** add azure service bus ([#601](https://github.com/digdir/dialogporten/issues/601)) ([4b008e1](https://github.com/digdir/dialogporten/commit/4b008e176dc9c9f4355b7b101c8a192e357e63fb))

## [1.4.0](https://github.com/digdir/dialogporten/compare/v1.3.0...v1.4.0) (2024-04-09)


### Features

* Split SeenLog from activities ([#598](https://github.com/digdir/dialogporten/issues/598)) ([71b77d2](https://github.com/digdir/dialogporten/commit/71b77d25ca464e4194712e45a19d16d30d17e4d5))
  * *This is a breaking change*, the `Seen` activity type has been removed, and all activities of this type is removed from the staging environment.
* Add EU endpoints for seen log ([#607](https://github.com/digdir/dialogporten/issues/607)) ([1aa7eeb](https://github.com/digdir/dialogporten/commit/1aa7eeb927d21fdea053640edcae7782e278c7cb))
  * `/api/v1/enduser/dialogs/{dialogId}/seenlog`
  * `/api/v1/enduser/dialogs/{dialogId}/seenlog/{seenLogId}`
  * `/api/v1/serviceowner/dialogs/{dialogId}/seenlog`
  * `/api/v1/serviceowner/dialogs/{dialogId}/seenlog/{seenLogId}`
* Add ExtendedStatus content type ([#589](https://github.com/digdir/dialogporten/issues/589)) ([a9f10b0](https://github.com/digdir/dialogporten/commit/a9f10b09ee58453766e1de46acd2166be48fd0b5))
* add fusion cache ([#579](https://github.com/digdir/dialogporten/issues/579)) ([973fa5c](https://github.com/digdir/dialogporten/commit/973fa5c9a68709f8ede71673fa96f06388cf2ea9))
* **azure:** copy from keyvault to app config ([#593](https://github.com/digdir/dialogporten/issues/593)) ([d216c90](https://github.com/digdir/dialogporten/commit/d216c9087472e4e93df913a56b58e79d71becab8))
* **service:** use in-memory transport instead of rabbitmq ([#602](https://github.com/digdir/dialogporten/issues/602)) ([dc339e7](https://github.com/digdir/dialogporten/commit/dc339e77e16c4e1014145a6811474ba15fef2b20))


### Bug Fixes

* Add PartyIdentifier.Separator to party validation error ([#595](https://github.com/digdir/dialogporten/issues/595)) ([14ee4a1](https://github.com/digdir/dialogporten/commit/14ee4a1a1cb57014d7d6d6ae385681fa5b3c690c))
* **azure:** ensure key vault url is correct and add keyvault readerrole for migration job ([#597](https://github.com/digdir/dialogporten/issues/597)) ([2f11a16](https://github.com/digdir/dialogporten/commit/2f11a164b6198848d965ae3a390ba0156204a6e7))

## [1.3.0](https://github.com/digdir/dialogporten/compare/v1.2.0...v1.3.0) (2024-04-03)


### Features

* Add db index for Dialog.Org ([#584](https://github.com/digdir/dialogporten/issues/584)) ([a4c1953](https://github.com/digdir/dialogporten/commit/a4c19530f238e61c046fa9cb8f3b366a7fefebad))


### Bug Fixes

* Require read action on elements without auth attr, replace unauthorized URLs ([#574](https://github.com/digdir/dialogporten/issues/574)) ([f39af31](https://github.com/digdir/dialogporten/commit/f39af3112391a6d6d6df42e6e5cc6d8115c07fd3))

## [1.2.0](https://github.com/digdir/dialogporten/compare/v1.1.1...v1.2.0) (2024-03-22)


### Features

* token issuer ([#556](https://github.com/digdir/dialogporten/issues/556)) ([d8165c1](https://github.com/digdir/dialogporten/commit/d8165c180f4190b3fa243181cd16aa3b007d35a4))

## [1.1.1](https://github.com/digdir/dialogporten/compare/v1.1.0...v1.1.1) (2024-03-22)


### Bug Fixes

* **azure:** avoid naming issue for secrets ([#572](https://github.com/digdir/dialogporten/issues/572)) ([50af860](https://github.com/digdir/dialogporten/commit/50af860d0037eae2611b59393974142f7e55f457))
* UpdateDialogEvent created when dialog element is deleted or updated ([#552](https://github.com/digdir/dialogporten/issues/552)) ([8d707ff](https://github.com/digdir/dialogporten/commit/8d707ffbe8a7a5ff70c053927d8c32cbf5f74410))

## [1.1.0](https://github.com/digdir/dialogporten/compare/v1.0.4...v1.1.0) (2024-03-13)


### Features

* Add name lookups ([#532](https://github.com/digdir/dialogporten/issues/532)) ([db9cadc](https://github.com/digdir/dialogporten/commit/db9cadca7b00bb98cee38dc2b154a36ff61b99ef))
* **azure:** add redis resource ([#518](https://github.com/digdir/dialogporten/issues/518)) ([1b2c013](https://github.com/digdir/dialogporten/commit/1b2c0133477bb454edcec4158eb1de1c7f2b8de7))
* use redis in web api ([#527](https://github.com/digdir/dialogporten/issues/527)) ([eabd708](https://github.com/digdir/dialogporten/commit/eabd7085b12b23ffa85ba2ce0901e033c33f0e35))


### Bug Fixes

* Allow for 2 seconds clock skew in token validation ([#536](https://github.com/digdir/dialogporten/issues/536)) ([a0147b8](https://github.com/digdir/dialogporten/commit/a0147b8035dfdaf3ba157e3967a396f5ed897e8c))
* **azure:** rename connection string key for redis ([#533](https://github.com/digdir/dialogporten/issues/533)) ([db36213](https://github.com/digdir/dialogporten/commit/db36213f1cd4b5b306844c4a333a7556449f01fc))
* **azure:** revert to using connection string for IDistributedCache Redis ([#526](https://github.com/digdir/dialogporten/issues/526)) ([d19350d](https://github.com/digdir/dialogporten/commit/d19350d6b3f08efc473f9f7459dd9ef00db83f67))
* **azure:** use built-in policy for redis ([#521](https://github.com/digdir/dialogporten/issues/521)) ([2a8fa76](https://github.com/digdir/dialogporten/commit/2a8fa76761a92fe76b694f93cf263ce4336ba39d))
* **azure:** use secret uri instead of host name in app config ([#522](https://github.com/digdir/dialogporten/issues/522)) ([7cafd77](https://github.com/digdir/dialogporten/commit/7cafd77c50dcd131fa86575cc3a0cd34b08881ea))
* **azure:** use SSL port for redis in connection string ([#546](https://github.com/digdir/dialogporten/issues/546)) ([548bc47](https://github.com/digdir/dialogporten/commit/548bc47729e805ab078c9e158e234f860523166f))
* Change IfMatchDialogRevision to Revision in DTO ([#535](https://github.com/digdir/dialogporten/issues/535)) ([3a065d3](https://github.com/digdir/dialogporten/commit/3a065d3aecd4c0740dbd15cef3bd0792f865b667))
* purge should accept any content-type and no body ([#540](https://github.com/digdir/dialogporten/issues/540)) ([736fb59](https://github.com/digdir/dialogporten/commit/736fb59e511aa6b52683611af64e5c60bc224772))
* remove prefix for redis connection string ([#541](https://github.com/digdir/dialogporten/issues/541)) ([ceb204c](https://github.com/digdir/dialogporten/commit/ceb204c4196688f8f458f656dd4f8356a4cda488))
* Update Altinn Authorization integration ([#457](https://github.com/digdir/dialogporten/issues/457)) ([#469](https://github.com/digdir/dialogporten/issues/469)) ([d0d846d](https://github.com/digdir/dialogporten/commit/d0d846d6f7b9ee69a0dc501aafb8bae463ab14a1))

## [1.0.4](https://github.com/digdir/dialogporten/compare/v1.0.3...v1.0.4) (2024-02-29)


### Bug Fixes

* add extra comment in dockerfile ([#503](https://github.com/digdir/dialogporten/issues/503)) ([77541cb](https://github.com/digdir/dialogporten/commit/77541cba45ae914c73e07856c8654732a97e86e5))

## [1.0.3](https://github.com/digdir/dialogporten/compare/v1.0.2...v1.0.3) (2024-02-28)


### Bug Fixes

* remove whiteline in dockerfile ([9b14994](https://github.com/digdir/dialogporten/commit/9b149949cfb36a527acf62318acaeadb3dca3fd1))

## [1.0.2](https://github.com/digdir/dialogporten/compare/v1.0.1...v1.0.2) (2024-02-28)


### Bug Fixes

* always run staging dry-runs in release-please-pr ([3e390e7](https://github.com/digdir/dialogporten/commit/3e390e758d2c1797ab7e4ae36f151a17de072662))
* fix workflow permissions ([40e5485](https://github.com/digdir/dialogporten/commit/40e5485bd3aa39150e482e58de9cb03a2b347d03))
* fix workflow permissions ([b2213b2](https://github.com/digdir/dialogporten/commit/b2213b29e71d82e60cec3b55fa476d77aee1639f))
* **release-please:** use correct gh token ([#500](https://github.com/digdir/dialogporten/issues/500)) ([ebff656](https://github.com/digdir/dialogporten/commit/ebff65611ed5f94b1fe42a9a288d7a2b1644d906))
* use temporary gh token ([c1118ae](https://github.com/digdir/dialogporten/commit/c1118ae436854d3aabff59689ad1a8a342b97df5))

## [1.0.1](https://github.com/digdir/dialogporten/compare/v1.0.0...v1.0.1) (2024-02-28)


### Bug Fixes

* remove main-tag when tagging docker images ([#498](https://github.com/digdir/dialogporten/issues/498)) ([ddc1bad](https://github.com/digdir/dialogporten/commit/ddc1badd6fe71de9abacf7dd8dbd1714abf3ba11))

## 1.0.0 (2024-02-28)


### Features

* Add element count to eu list dto ([#414](https://github.com/digdir/dialogporten/issues/414)) ([934fa93](https://github.com/digdir/dialogporten/commit/934fa93b9b272684a160855a048c7fae3aa39f81))
* Add purge functionallity separate from soft delete. ([#483](https://github.com/digdir/dialogporten/issues/483)) ([1349efb](https://github.com/digdir/dialogporten/commit/1349efb8c81a4ba40703b49ad716116834f3180f))
* Add SeenBy per user ([#368](https://github.com/digdir/dialogporten/issues/368)) ([c68db9e](https://github.com/digdir/dialogporten/commit/c68db9eed984c175e90ecd29e1374cc9aeed1863))
* **azure:** parameterize SKUs ([#364](https://github.com/digdir/dialogporten/issues/364)) ([9c27c74](https://github.com/digdir/dialogporten/commit/9c27c744784ec29c95b969871e7807f96b288c03))
* change format of party identifier ([#376](https://github.com/digdir/dialogporten/issues/376)) ([27e6744](https://github.com/digdir/dialogporten/commit/27e674447331348b520a983fab0ea03e782afcaa)), closes [#220](https://github.com/digdir/dialogporten/issues/220)
* Container app revision verification on deploy ([#392](https://github.com/digdir/dialogporten/issues/392)) ([db13a89](https://github.com/digdir/dialogporten/commit/db13a8955e39b3012efd020cd62171a54a9b1ddb))
* Slack notifier IaC ([#341](https://github.com/digdir/dialogporten/issues/341)) ([80c3579](https://github.com/digdir/dialogporten/commit/80c35795278377089f0fe25248dfe8630fb358b7))


### Bug Fixes

* 412 status on multiple requests without revision header ([#427](https://github.com/digdir/dialogporten/issues/427)) ([047cf71](https://github.com/digdir/dialogporten/commit/047cf71945b60839d4de544041f42f1e6ff884f8))
* add APIM base uri for dialogporten ([948b9a4](https://github.com/digdir/dialogporten/commit/948b9a46ef503bf4171d80d50610da8606af37c2))
* add apim base uri for staging ([#451](https://github.com/digdir/dialogporten/issues/451)) ([580d946](https://github.com/digdir/dialogporten/commit/580d94604a089d689ab7783f5821481456b88de6))
* add base uri for web api ([#425](https://github.com/digdir/dialogporten/issues/425)) ([0aa941b](https://github.com/digdir/dialogporten/commit/0aa941bcb4a911ed1866e6c37c2c061af7db9ebd))
* add correct APIM base uri for dialogporten ([713771a](https://github.com/digdir/dialogporten/commit/713771a0248ed30a7336a09438f8f016bde106ef))
* add correct baseuri for altinn events ([#496](https://github.com/digdir/dialogporten/issues/496)) ([74940ab](https://github.com/digdir/dialogporten/commit/74940abfdc64a16a6c88bf432fbbf0725f3c4f5e))
* Add null checks, set lists to empty if null ([#434](https://github.com/digdir/dialogporten/issues/434)) ([f264aec](https://github.com/digdir/dialogporten/commit/f264aeca0e3eeee7e79356cbd1ae78107a1f1872))
* **azure:** fix postgresql auth config ([#357](https://github.com/digdir/dialogporten/issues/357)) ([4a4757f](https://github.com/digdir/dialogporten/commit/4a4757fa9a2e7e7cd6e958b7df222c10f99a388a))
* **azure:** remove default value for KEY_VAULT_SOURCE_KEYS ([#418](https://github.com/digdir/dialogporten/issues/418)) ([b0d74e8](https://github.com/digdir/dialogporten/commit/b0d74e81ba846e35c52f230f0cf7bc9078461607))
* **azure:** remove default values in params and ensure secure on params ([#415](https://github.com/digdir/dialogporten/issues/415)) ([94b9885](https://github.com/digdir/dialogporten/commit/94b98856b36df02fdf95237f0870127c919c38eb))
* **azure:** rename and fix outputs and pass correct secrets ([#416](https://github.com/digdir/dialogporten/issues/416)) ([68f0c8b](https://github.com/digdir/dialogporten/commit/68f0c8b06a0bacb23498ccd67b58bce4b116a79e))
* build errors for 8.0.200 ([#440](https://github.com/digdir/dialogporten/issues/440)) ([b133f8f](https://github.com/digdir/dialogporten/commit/b133f8fd686a73ed8a308a15941c5ddb9e2a5375))
* Check Content for null, use DependentRules, disallow empty localization values ([#413](https://github.com/digdir/dialogporten/issues/413)) ([894644a](https://github.com/digdir/dialogporten/commit/894644abee5e5d38fec3dcc1614bf13bc57efe13))
* Correct params for revision verification ([#405](https://github.com/digdir/dialogporten/issues/405)) ([4b98348](https://github.com/digdir/dialogporten/commit/4b98348c567b59347ad6c9aad4f19a4c6cd2a92d))
* Do not allow empty content ([#436](https://github.com/digdir/dialogporten/issues/436)) ([a083544](https://github.com/digdir/dialogporten/commit/a083544ed56911d89095abbfd0942c4333d0a1e8))
* do not prefix swagger document in development ([#491](https://github.com/digdir/dialogporten/issues/491)) ([e330ce3](https://github.com/digdir/dialogporten/commit/e330ce3998bc0fdb17b277abaa9aef27b5516361))
* remove path to swagger json ([fe1e770](https://github.com/digdir/dialogporten/commit/fe1e7706d55e55768899266a8a5f951a357358a5))
* rename migration job ([#423](https://github.com/digdir/dialogporten/issues/423)) ([3897db2](https://github.com/digdir/dialogporten/commit/3897db2e1b04c3aec70cb098abf6a2d6341ee750))
* restrict container apps to apim ip ([#448](https://github.com/digdir/dialogporten/issues/448)) ([1a1f3ad](https://github.com/digdir/dialogporten/commit/1a1f3ad9dbf12e7633e3ec89c4d3554109714b5e))
* Return 410 Gone when updating deleted dialog ([#464](https://github.com/digdir/dialogporten/issues/464)) ([2498b0a](https://github.com/digdir/dialogporten/commit/2498b0a6aa7c0901ba6d41ed95e99d66a5a6a0cd))
* set base path for swagger json ui ([476fdca](https://github.com/digdir/dialogporten/commit/476fdca40a1d59c3348be39cb8b97461aa57225d))
* set base url for swagger json ([#447](https://github.com/digdir/dialogporten/issues/447)) ([2161066](https://github.com/digdir/dialogporten/commit/2161066866ff124f6e1f3d7b11bac17de4da3d25))
* shorten secret name for container app job ([#422](https://github.com/digdir/dialogporten/issues/422)) ([09b2f30](https://github.com/digdir/dialogporten/commit/09b2f307bcc96f1cc2952c44cdd1c9e30c93c699))
* try echoing pgpassword in migration job🤫 ([#419](https://github.com/digdir/dialogporten/issues/419)) ([fe673a3](https://github.com/digdir/dialogporten/commit/fe673a37a35d409f4706dbc8de31c32373edcba2))
* Use data from events, not from db ([#455](https://github.com/digdir/dialogporten/issues/455)) ([469c606](https://github.com/digdir/dialogporten/commit/469c606f6a387a6cf16ee1567e2dc3d59d86373f))

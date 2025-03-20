const localBaseUrl = "https://localhost:7214/";
const localDockerBaseUrl = "https://host.docker.internal:7214/";
const testBaseUrl = "https://altinn-dev-api.azure-api.net/dialogporten/";
const yt01BaseUrl = "https://platform.yt01.altinn.cloud/dialogporten/";
const stagingBaseUrl = "https://platform.tt02.altinn.no/dialogporten/";
const prodBaseUrl = "https://platform.altinn.no/dialogporten/";

const endUserPath = "api/v1/enduser/";
const serviceOwnerPath = "api/v1/serviceowner/";
const graphqlPath = "graphql";

export const baseUrls = {
    v1: {
        enduser: {
            localdev: localBaseUrl + endUserPath,
            localdev_docker: localDockerBaseUrl + endUserPath,
            test: testBaseUrl + endUserPath,
            yt01: yt01BaseUrl + endUserPath,
            staging: stagingBaseUrl + endUserPath,
            prod: prodBaseUrl + endUserPath
        },
        serviceowner: {
            localdev: localBaseUrl + serviceOwnerPath,
            localdev_docker: localDockerBaseUrl + serviceOwnerPath,
            test: testBaseUrl + serviceOwnerPath,
            yt01: yt01BaseUrl + serviceOwnerPath,
            staging: stagingBaseUrl + serviceOwnerPath,
            prod: prodBaseUrl + serviceOwnerPath
        },
        graphql: {
            localdev: localBaseUrl + graphqlPath,
            localdev_docker: localDockerBaseUrl + graphqlPath,
            test: testBaseUrl + graphqlPath,
            yt01: yt01BaseUrl + graphqlPath,
            staging: stagingBaseUrl + graphqlPath,
            prod: prodBaseUrl + graphqlPath
        },
    }
};

if (__ENV.IS_DOCKER && __ENV.API_ENVIRONMENT == "localdev") {
    __ENV.API_ENVIRONMENT = "localdev_docker";
}

export const defaultEndUserOrgNo = "310923044"; // ÆRLIG UROKKELIG TIGER AS
export const defaultEndUserSsn = "08844397713"; // UROMANTISK LITTERATUR, has "DAGL" for 310923044
export const defaultServiceOwnerOrgNo = __ENV.API_ENVIRONMENT == "yt01" ? "713431400" : "991825827";
export const otherOrg = (() => {
    switch (__ENV.API_ENVIRONMENT) {
        case 'test':
        case 'localdev':
        case 'localdev_docker':    
            return {
                orgNo: '974760673',
                name: 'brg',
                serviceResource: 'brg-dialogporten-automated-tests'
            };
        case 'yt01':
            return {
                orgNo: '974761076',
                name: 'skd', 
                serviceResource: 'app_skd_formueinntekt-skattemelding-v2'
            };
        case 'staging':
        case 'prod':    
            return {
                orgNo: '889640782',
                name: 'nav',
                serviceResource: 'app_nav_barnehagelister'
            }; 
        default:
            throw new Error(`Invalid API environment: ${__ENV.API_ENVIRONMENT}. Please ensure it's set correctly in your environment variables.`);
    }
})();

export const otherOrgNo = otherOrg.orgNo;
export const otherOrgName = otherOrg.name;
export const otherServiceResource = otherOrg.serviceResource;
export const notValidEnduserId = __ENV.API_ENVIRONMENT == "yt01" ? "08837297959" : "08895699684";

if (!baseUrls[__ENV.API_VERSION]) {
    throw new Error(`Invalid API version: ${__ENV.API_VERSION}. Please ensure it's set correctly in your environment variables.`);
}

if (!baseUrls[__ENV.API_VERSION]["enduser"][__ENV.API_ENVIRONMENT]) {
    throw new Error(`Invalid enduser API environment: ${__ENV.API_ENVIRONMENT}. Please ensure it's set correctly in your environment variables.`);
}

if (!baseUrls[__ENV.API_VERSION]["serviceowner"][__ENV.API_ENVIRONMENT]) {
    throw new Error(`Invalid enduser API environment: ${__ENV.API_ENVIRONMENT}. Please ensure it's set correctly in your environment variables.`);
}

export const baseUrlEndUser = baseUrls[__ENV.API_VERSION]["enduser"][__ENV.API_ENVIRONMENT];
export const baseUrlServiceOwner = baseUrls[__ENV.API_VERSION]["serviceowner"][__ENV.API_ENVIRONMENT];
export const tokenGeneratorEnv = (() => {
    switch (__ENV.API_ENVIRONMENT) {
        case 'localdev':
        case 'localdev_docker':
        case 'test':
            return 'at23';
        case 'staging':
            return 'tt02';
        case 'yt01':
            return 'yt01';
        case 'prod':
            return 'tt02';
        default:
            throw new Error(`Invalid API environment: ${__ENV.API_ENVIRONMENT}. Please ensure it's set correctly in your environment variables.`);
    }
})();

export const baseUrlGraphql = baseUrls[__ENV.API_VERSION]["graphql"][__ENV.API_ENVIRONMENT];

export const sentinelValue = "dialogporten-e2e-sentinel";
export const sentinelPerformanceValue = "dialogporten-performance-sentinel";

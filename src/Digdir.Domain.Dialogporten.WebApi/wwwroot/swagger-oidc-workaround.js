/**
 * Modify the query-parameters when doing the OAuth2 authorization request.
 * - We need to add a nonce for OIDC
 * - Add the prompt=login to always prompt a login in case logout doesn't work
 */
const oriOpen = window.open;
window.open = function (...args) {
    const url = new URL(args[0]);
    const hasOpenId = url
        .searchParams
        .get('scope')
        ?.split(' ')
        ?.some(x => x === 'openid');

    if (hasOpenId) {
        url.searchParams.set('nonce', crypto.randomUUID());
        url.searchParams.set('prompt', "login");
        args[0] = url.toString();
    }

    return oriOpen.apply(window, args);
}

/**
 * Attack a global click listener that scans for logout button clicks
 * If we clicked a logout button, navigate to the OIDC logout endpoint.
 */
document.addEventListener('click', (e) => {
    const btn = e.target.closest('button');
    if (!btn) return;
    if (!(/logout/i.test(btn.textContent))) return;
    if (!ui.getState().hasIn(['auth', 'authorized', 'AuthorizationCode', 'clientId'])) return;

    const logoutUrl = ui.getConfigs().SWAGGER_IDPORTEN_LOGOUT_URL?.replace(/\+$/, "");
    if (!logoutUrl) {
        console.error("SWAGGER_IDPORTEN_LOGOUT_URL is missing, can't log out properly")
        return;
    }

    const clientId = ui.getState().getIn(['auth', 'authorized', 'AuthorizationCode', 'clientId']);
    const logoutRedirectUri = `${encodeURIComponent(window.location.origin)}/swagger/index.html`;
    const params = {
        "client_id": clientId,
        "post_logout_redirect_uri": logoutRedirectUri,
    }
    const queryString = Object
        .keys(params)
        .filter(key => params[key])
        .map(key => `${key}=${params[key]}`)
        .join('&');

    window.location.href = `${logoutUrl}?${queryString}`;
}, true);

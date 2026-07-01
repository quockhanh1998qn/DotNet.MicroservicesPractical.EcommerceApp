export const environment = {
  production: false,
  apiBase: 'http://localhost:5000',
  identity: {
    issuer: 'http://localhost:5009',
    clientId: 'webapp_angular',
    redirectUri: 'http://localhost:4200/auth-callback',
    silentRefreshRedirectUri: 'http://localhost:4200/silent-renew',
    postLogoutRedirectUri: 'http://localhost:4200/',
    responseType: 'code',
    scope: 'openid profile email roles offline_access product.api customer.api basket.api ordering.api inventory.api',
  },
};

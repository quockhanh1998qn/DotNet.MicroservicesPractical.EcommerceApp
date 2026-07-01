import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthConfig, OAuthService } from 'angular-oauth2-oidc';
import { environment } from '@env';

const authCodeFlowConfig: AuthConfig = {
  issuer: environment.identity.issuer,
  redirectUri: environment.identity.redirectUri,
  silentRefreshRedirectUri: environment.identity.silentRefreshRedirectUri,
  postLogoutRedirectUri: environment.identity.postLogoutRedirectUri,
  clientId: environment.identity.clientId,
  responseType: environment.identity.responseType,
  scope: environment.identity.scope,
  showDebugInformation: !environment.production,
  requireHttps: false,
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauth = inject(OAuthService);

  private readonly _identity = signal<{ sub: string; name?: string; email?: string; roles: string[] } | null>(null);
  readonly identity = this._identity.asReadonly();
  readonly isAuthenticated = computed(() => this._identity() !== null);
  readonly isAdmin = computed(() => this._identity()?.roles.includes('Admin') ?? false);

  async init(): Promise<void> {
    this.oauth.configure(authCodeFlowConfig);
    this.oauth.setupAutomaticSilentRefresh();
    try {
      await this.oauth.loadDiscoveryDocumentAndTryLogin();
    } catch (err) {
      console.warn('[AuthService] OIDC discovery failed; continuing unauthenticated.', err);
    }
    this.refreshIdentity();
    this.oauth.events.subscribe(() => this.refreshIdentity());
  }

  login(): void { this.oauth.initCodeFlow(); }
  logout(): void { this.oauth.logOut(); }
  get accessToken(): string | null { return this.oauth.getAccessToken() || null; }

  private refreshIdentity(): void {
    if (!this.oauth.hasValidAccessToken()) { this._identity.set(null); return; }
    const idClaims = (this.oauth.getIdentityClaims() ?? {}) as Record<string, unknown>;
    const accessClaims = this.decodeAccessToken();
    // Merge: id_token claims take precedence, fall back to access_token claims (Duende ApiResource user claims).
    const claims: Record<string, unknown> = { ...accessClaims, ...idClaims };
    if (!claims['sub']) { this._identity.set(null); return; }
    const role = claims['role'];
    const roles = Array.isArray(role) ? (role as string[]) : role ? [role as string] : [];
    this._identity.set({
      sub: claims['sub'] as string,
      name: (claims['name'] ?? claims['preferred_username']) as string | undefined,
      email: (claims['email'] ?? claims['preferred_username']) as string | undefined,
      roles,
    });
  }

  private decodeAccessToken(): Record<string, unknown> {
    const token = this.accessToken;
    if (!token) return {};
    const parts = token.split('.');
    if (parts.length < 2) return {};
    try {
      const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = payload + '==='.slice((payload.length + 3) % 4);
      const json = atob(padded);
      return JSON.parse(json) as Record<string, unknown>;
    } catch {
      return {};
    }
  }
}

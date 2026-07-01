import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { adminGuard, authGuard } from './auth.guard';
import { AuthService } from './auth.service';

class AuthServiceStub {
  authenticated = false;
  admin = false;
  loginCalled = false;
  isAuthenticated = () => this.authenticated;
  isAdmin = () => this.admin;
  login = () => { this.loginCalled = true; };
}

const route = {} as ActivatedRouteSnapshot;
const state = { url: '/secret' } as RouterStateSnapshot;

describe('authGuard', () => {
  let auth: AuthServiceStub;
  let router: Router;

  beforeEach(() => {
    auth = new AuthServiceStub();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: { createUrlTree: (cmds: unknown[]) => ({ cmds } as unknown as UrlTree) } },
      ],
    });
    router = TestBed.inject(Router);
  });

  it('grants access when authenticated', () => {
    auth.authenticated = true;
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));
    expect(result).toBe(true);
    expect(auth.loginCalled).toBe(false);
  });

  it('triggers login + redirects to / when not authenticated', () => {
    auth.authenticated = false;
    const result = TestBed.runInInjectionContext(() => authGuard(route, state)) as UrlTree;
    expect(auth.loginCalled).toBe(true);
    expect((result as unknown as { cmds: unknown[] }).cmds).toEqual(['/']);
    expect(router).toBeTruthy();
  });
});

describe('adminGuard', () => {
  let auth: AuthServiceStub;

  beforeEach(() => {
    auth = new AuthServiceStub();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: { createUrlTree: (cmds: unknown[]) => ({ cmds }) } },
      ],
    });
  });

  it('allows when caller has Admin role', () => {
    auth.admin = true;
    expect(TestBed.runInInjectionContext(() => adminGuard(route, state))).toBe(true);
  });

  it('redirects to / when caller is not admin', () => {
    auth.admin = false;
    const result = TestBed.runInInjectionContext(() => adminGuard(route, state)) as unknown as { cmds: unknown[] };
    expect(result.cmds).toEqual(['/']);
  });
});

import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { AuthService } from './auth.service';
import { authGuard, permissionGuard, portalGuard, professionalGuard } from './auth.guards';
import { OrganizationContextService } from './organization-context.service';

describe('authentication guards', () => {
  const authenticated = signal(false);
  const professionalAccess = signal(false);
  const portalAccess = signal(false);
  let router: Router;

  beforeEach(() => {
    authenticated.set(false);
    professionalAccess.set(false);
    portalAccess.set(false);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: authenticated.asReadonly(),
            hasProfessionalAccess: professionalAccess.asReadonly(),
            hasPortalAccess: portalAccess.asReadonly(),
          },
        },
        {
          provide: OrganizationContextService,
          useValue: {
            hasPermission: (permission: string) => permission === 'events.view',
          },
        },
      ],
    });
    router = TestBed.inject(Router);
  });

  it('keeps the requested URL when redirecting to login', () => {
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/app/events' } as RouterStateSnapshot),
    );

    expect(router.serializeUrl(result as UrlTree)).toBe('/auth/login?returnUrl=%2Fapp%2Fevents');
  });

  it('allows authenticated users', () => {
    authenticated.set(true);

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/app' } as RouterStateSnapshot),
    );

    expect(result).toBe(true);
  });

  it('separates professional and client portal access', () => {
    const professionalRedirect = TestBed.runInInjectionContext(() =>
      professionalGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
    expect(router.serializeUrl(professionalRedirect as UrlTree)).toBe('/portal/events');

    const portalRedirect = TestBed.runInInjectionContext(() =>
      portalGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
    expect(router.serializeUrl(portalRedirect as UrlTree)).toBe('/app/dashboard');

    professionalAccess.set(true);
    portalAccess.set(true);
    expect(
      TestBed.runInInjectionContext(() =>
        professionalGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
      ),
    ).toBe(true);
    expect(
      TestBed.runInInjectionContext(() =>
        portalGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
      ),
    ).toBe(true);
  });

  it('allows only routes whose required permission is effective', () => {
    const allowed = TestBed.runInInjectionContext(() =>
      permissionGuard(
        {
          data: { permission: 'events.view' },
        } as unknown as ActivatedRouteSnapshot,
        {} as RouterStateSnapshot,
      ),
    );
    const denied = TestBed.runInInjectionContext(() =>
      permissionGuard(
        {
          data: { permission: 'events.update' },
        } as unknown as ActivatedRouteSnapshot,
        {} as RouterStateSnapshot,
      ),
    );

    expect(allowed).toBe(true);
    expect(router.serializeUrl(denied as UrlTree)).toBe('/app/dashboard');
  });
});

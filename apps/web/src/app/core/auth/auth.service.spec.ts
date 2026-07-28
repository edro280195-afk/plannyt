import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { ApiService } from '../api/api.service';
import { AuthResponse, MeResponse } from '../models/api.models';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  const authResponse: AuthResponse = {
    accessToken: 'access-token',
    accessTokenExpiresAt: '2026-07-28T18:00:00Z',
    userAccountId: 'user-1',
    email: 'planner@plannyt.mx',
    organizationId: 'org-1',
  };
  const meResponse: MeResponse = {
    userAccountId: 'user-1',
    email: 'planner@plannyt.mx',
    organizations: [
      {
        organizationId: 'org-1',
        organizationName: 'Armonía Eventos',
        membershipId: 'membership-1',
        role: 'Owner',
        permissions: ['events.read', 'events.write'],
      },
    ],
    eventAccesses: [],
  };
  let api: {
    login: ReturnType<typeof vi.fn>;
    registerPlanner: ReturnType<typeof vi.fn>;
    registerAndAcceptInvitation: ReturnType<typeof vi.fn>;
    refresh: ReturnType<typeof vi.fn>;
    getMe: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
    logoutAll: ReturnType<typeof vi.fn>;
  };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let service: AuthService;

  beforeEach(() => {
    api = {
      login: vi.fn(),
      registerPlanner: vi.fn(),
      registerAndAcceptInvitation: vi.fn(),
      refresh: vi.fn(),
      getMe: vi.fn(),
      logout: vi.fn(),
      logoutAll: vi.fn(),
    };
    router = { navigate: vi.fn().mockResolvedValue(true) };
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: ApiService, useValue: api },
        { provide: Router, useValue: router },
      ],
    });
    service = TestBed.inject(AuthService);
  });

  it('keeps the access token in memory after login', () => {
    api.login.mockReturnValue(of(authResponse));
    api.getMe.mockReturnValue(of(meResponse));
    let actual: MeResponse | undefined;

    service
      .login({
        email: 'planner@plannyt.mx',
        password: 'a-secure-password',
        isPersistent: true,
      })
      .subscribe((me) => {
        actual = me;
      });

    expect(actual).toEqual(meResponse);
    expect(service.accessToken()).toBe('access-token');
    expect(service.me()).toEqual(meResponse);
    expect(service.hasProfessionalAccess()).toBe(true);
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  it('restores a valid cookie session and loads the profile', () => {
    api.refresh.mockReturnValue(of(authResponse));
    api.getMe.mockReturnValue(of(meResponse));
    let completed = false;

    service.restore().subscribe(() => {
      completed = true;
    });

    expect(completed).toBe(true);
    expect(service.restoring()).toBe(false);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.primaryOrganization()?.organizationId).toBe('org-1');
  });

  it('finishes anonymous when session restoration fails', () => {
    api.refresh.mockReturnValue(throwError(() => new Error('No refresh cookie')));

    service.restore().subscribe();

    expect(service.restoring()).toBe(false);
    expect(service.accessToken()).toBeNull();
    expect(service.me()).toBeNull();
  });

  it('shares one refresh request between concurrent subscribers', () => {
    const response = new Subject<AuthResponse>();
    api.refresh.mockReturnValue(response.asObservable());

    const first = service.refreshSession();
    const second = service.refreshSession();
    first.subscribe();
    second.subscribe();

    expect(first).toBe(second);
    expect(api.refresh).toHaveBeenCalledTimes(1);

    response.next(authResponse);
    response.complete();
    expect(service.accessToken()).toBe('access-token');
  });

  it('clears local state and returns to login after logout', () => {
    api.login.mockReturnValue(of(authResponse));
    api.getMe.mockReturnValue(of(meResponse));
    api.logout.mockReturnValue(of(undefined));
    service
      .login({
        email: 'planner@plannyt.mx',
        password: 'a-secure-password',
        isPersistent: false,
      })
      .subscribe();

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
  });
});

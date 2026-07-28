import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../models/api.models';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let token: string | null;
  let auth: {
    accessToken: () => string | null;
    refreshSession: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    token = 'original-token';
    auth = {
      accessToken: () => token,
      refreshSession: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controller.verify();
  });

  it('adds the bearer token and credentials to API requests', () => {
    http.get(`${environment.apiBaseUrl}/events`).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/events`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer original-token');
    expect(request.request.withCredentials).toBe(true);
    request.flush([]);
  });

  it('does not alter requests outside the API', () => {
    http.get('/manifest.webmanifest').subscribe();

    const request = controller.expectOne('/manifest.webmanifest');
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(request.request.withCredentials).toBe(false);
    request.flush({});
  });

  it('marks refresh operations without sending an access token', () => {
    http.post(`${environment.apiBaseUrl}/auth/refresh`, null).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/auth/refresh`);
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(request.request.headers.get('X-Plannyt-Client')).toBe('web');
    expect(request.request.withCredentials).toBe(true);
    request.flush({});
  });

  it('refreshes once and retries a protected request after a 401', () => {
    const refreshed: AuthResponse = {
      accessToken: 'renewed-token',
      accessTokenExpiresAt: '2026-07-28T18:00:00Z',
      userAccountId: 'user-1',
      email: 'planner@plannyt.mx',
      organizationId: 'org-1',
    };
    auth.refreshSession.mockImplementation(() => {
      token = refreshed.accessToken;
      return of(refreshed);
    });
    let response: { ok: boolean } | undefined;

    http.get<{ ok: boolean }>(`${environment.apiBaseUrl}/auth/me`).subscribe((value) => {
      response = value;
    });

    const initial = controller.expectOne(`${environment.apiBaseUrl}/auth/me`);
    initial.flush({ title: 'No autorizado' }, { status: 401, statusText: 'Unauthorized' });

    const retried = controller.expectOne(`${environment.apiBaseUrl}/auth/me`);
    expect(retried.request.headers.get('Authorization')).toBe('Bearer renewed-token');
    retried.flush({ ok: true });

    expect(auth.refreshSession).toHaveBeenCalledTimes(1);
    expect(response).toEqual({ ok: true });
  });
});

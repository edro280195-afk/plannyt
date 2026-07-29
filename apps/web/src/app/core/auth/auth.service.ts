import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  catchError,
  finalize,
  map,
  Observable,
  of,
  shareReplay,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import {
  AuthResponse,
  LoginRequest,
  MeResponse,
  RegisterAndAcceptInvitationRequest,
  RegisterPlannerRequest,
} from '../models/api.models';
import { ApiService } from '../api/api.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private static readonly SessionEventKey = 'plannyt_session_event';

  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly accessTokenState = signal<string | null>(null);
  private readonly meState = signal<MeResponse | null>(null);
  private refreshRequest: Observable<AuthResponse> | null = null;

  readonly accessToken = this.accessTokenState.asReadonly();
  readonly me = this.meState.asReadonly();
  readonly restoring = signal(true);
  readonly isAuthenticated = computed(() => this.accessTokenState() !== null);
  readonly primaryOrganization = computed(() => this.meState()?.organizations[0] ?? null);
  readonly hasProfessionalAccess = computed(() => (this.meState()?.organizations.length ?? 0) > 0);
  readonly hasPortalAccess = computed(() => (this.meState()?.eventAccesses.length ?? 0) > 0);

  constructor() {
    const handleSessionEvent = (event: StorageEvent): void => {
      if (event.key === AuthService.SessionEventKey && event.newValue?.startsWith('logout:')) {
        this.clearSession();
        void this.router.navigate(['/auth/login']);
      }
    };

    window.addEventListener('storage', handleSessionEvent);
    this.destroyRef.onDestroy(() => window.removeEventListener('storage', handleSessionEvent));
  }

  restore(): Observable<void> {
    this.restoring.set(true);
    return this.refreshSession().pipe(
      switchMap(() => this.reloadMe()),
      map(() => undefined),
      catchError(() => {
        this.clearSession();
        return of(undefined);
      }),
      finalize(() => this.restoring.set(false)),
    );
  }

  login(request: LoginRequest): Observable<MeResponse> {
    return this.api.login(request).pipe(
      tap((response) => this.applyAuth(response)),
      switchMap(() => this.reloadMe()),
    );
  }

  registerPlanner(request: RegisterPlannerRequest): Observable<MeResponse> {
    return this.api.registerPlanner(request).pipe(
      tap((response) => this.applyAuth(response)),
      switchMap(() => this.reloadMe()),
    );
  }

  registerAndAcceptInvitation(
    token: string,
    request: RegisterAndAcceptInvitationRequest,
  ): Observable<MeResponse> {
    return this.api.registerAndAcceptInvitation(token, request).pipe(
      tap((response) => this.applyAuth(response)),
      switchMap(() => this.reloadMe()),
    );
  }

  refreshSession(): Observable<AuthResponse> {
    if (this.refreshRequest) {
      return this.refreshRequest;
    }

    this.refreshRequest = this.api.refresh().pipe(
      tap((response) => this.applyAuth(response)),
      catchError((error: unknown) => {
        this.clearSession();
        return throwError(() => error);
      }),
      finalize(() => {
        this.refreshRequest = null;
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.refreshRequest;
  }

  reloadMe(): Observable<MeResponse> {
    return this.api.getMe().pipe(tap((me) => this.meState.set(me)));
  }

  logout(): void {
    this.api
      .logout()
      .pipe(
        catchError(() => of(undefined)),
        finalize(() => {
          this.finishLogout();
        }),
      )
      .subscribe();
  }

  logoutAll(): Observable<void> {
    return this.api.logoutAll().pipe(
      finalize(() => {
        this.finishLogout();
      }),
    );
  }

  clearSession(): void {
    this.accessTokenState.set(null);
    this.meState.set(null);
  }

  private applyAuth(response: AuthResponse): void {
    this.accessTokenState.set(response.accessToken);
  }

  private finishLogout(): void {
    this.clearSession();
    this.broadcastLogout();
    void this.router.navigate(['/auth/login']);
  }

  private broadcastLogout(): void {
    try {
      localStorage.setItem(AuthService.SessionEventKey, `logout:${Date.now().toString()}`);
      localStorage.removeItem(AuthService.SessionEventKey);
    } catch {
      // El backend revoca la sesión aunque el navegador bloquee Storage.
    }
  }
}

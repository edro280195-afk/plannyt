import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { InvitationPublic } from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-invitation-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="invite-page">
      <a class="brand brand--center" routerLink="/auth/login">
        <span class="brand__mark">P</span>
        <strong>Plannyt</strong>
      </a>

      <section class="invite-card">
        @if (loading()) {
          <div class="skeleton skeleton--hero"></div>
        } @else if (invitation(); as invite) {
          <span class="invite-card__icon">✦</span>
          <span class="eyebrow">Tienes una invitación</span>
          <h1>
            {{ invite.eventName ?? invite.organizationName }}
          </h1>
          <p>
            <strong>{{ invite.organizationName }}</strong> te invita como
            {{ roleLabel(invite.intendedRole) }}.
          </p>
          <div class="invite-summary">
            <span>
              <small>Correo objetivo</small>
              <strong>{{ invite.targetEmail }}</strong>
            </span>
            <span>
              <small>Vigencia</small>
              <strong>{{ invite.expiresAt | date: 'medium' }}</strong>
            </span>
          </div>

          @if (invite.status !== 'Pending') {
            <div class="info-callout info-callout--warning">
              <span>!</span>
              <p>Esta invitación está {{ statusLabel(invite.status) }} y ya no puede utilizarse.</p>
            </div>
          } @else if (auth.isAuthenticated()) {
            <form [formGroup]="profileForm" (ngSubmit)="acceptExisting()" class="form-grid">
              <label>Nombre <input formControlName="firstName" /></label>
              <label>Apellido <input formControlName="lastName" /></label>
              <label>Teléfono <input formControlName="contactPhone" /></label>
              <label>Zona horaria <input formControlName="timeZone" /></label>
              <button
                class="btn btn--primary btn--large span-2"
                type="submit"
                [disabled]="submitting()"
              >
                {{ submitting() ? 'Aceptando…' : 'Aceptar con mi cuenta' }}
              </button>
            </form>
          } @else {
            <form [formGroup]="registerForm" (ngSubmit)="registerAndAccept()" class="form-grid">
              <label>Nombre <input formControlName="firstName" /></label>
              <label>Apellido <input formControlName="lastName" /></label>
              <label>Teléfono <input formControlName="contactPhone" /></label>
              <label>Zona horaria <input formControlName="timeZone" /></label>
              <label class="span-2">
                Crea una contraseña
                <input type="password" formControlName="password" autocomplete="new-password" />
                <small>Entre 12 y 128 caracteres.</small>
              </label>
              <button
                class="btn btn--primary btn--large span-2"
                type="submit"
                [disabled]="registerForm.invalid || submitting()"
              >
                {{ submitting() ? 'Creando acceso…' : 'Crear cuenta y aceptar' }}
              </button>
            </form>
            <p class="auth-switch">
              ¿Ya tienes cuenta?
              <a
                [routerLink]="['/auth/login']"
                [queryParams]="{ returnUrl: '/accept-access/' + token }"
              >
                Inicia sesión
              </a>
            </p>
          }
        } @else {
          <span class="invite-card__icon invite-card__icon--muted">?</span>
          <h1>Invitación no disponible</h1>
          <p>Revisa el enlace o solicita uno nuevo a la organización.</p>
        }
      </section>
    </main>
  `,
})
export class InvitationPage {
  private readonly api = inject(ApiService);
  protected readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly token =
    this.route.snapshot.paramMap.get('token') ??
    (() => {
      throw new Error('La invitación requiere token.');
    })();
  protected readonly invitation = signal<InvitationPublic | null>(null);
  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  private readonly defaultTimeZone =
    Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Matamoros';
  protected readonly registerForm = new FormGroup({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    contactPhone: new FormControl('', { nonNullable: true }),
    preferredLanguage: new FormControl('es', { nonNullable: true }),
    timeZone: new FormControl(this.defaultTimeZone, {
      nonNullable: true,
      validators: [Validators.required],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(12), Validators.maxLength(128)],
    }),
  });
  protected readonly profileForm = new FormGroup({
    firstName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true }),
    contactPhone: new FormControl('', { nonNullable: true }),
    preferredLanguage: new FormControl('es', { nonNullable: true }),
    timeZone: new FormControl(this.defaultTimeZone, { nonNullable: true }),
  });

  constructor() {
    this.api
      .getInvitation(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invitation) => {
          this.invitation.set(invitation);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  protected registerAndAccept(): void {
    if (this.registerForm.invalid || this.submitting()) {
      return;
    }

    const value = this.registerForm.getRawValue();
    this.submitting.set(true);
    this.auth
      .registerAndAcceptInvitation(this.token, {
        ...value,
        contactPhone: value.contactPhone || null,
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (me) => this.navigateAfterAcceptance(me.organizations.length > 0),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected acceptExisting(): void {
    if (this.submitting()) {
      return;
    }

    const value = this.profileForm.getRawValue();
    this.submitting.set(true);
    this.api
      .acceptInvitation(this.token, {
        firstName: value.firstName || null,
        lastName: value.lastName || null,
        contactPhone: value.contactPhone || null,
        preferredLanguage: value.preferredLanguage || null,
        timeZone: value.timeZone || null,
      })
      .pipe(
        switchMap(() => this.auth.reloadMe()),
        finalize(() => this.submitting.set(false)),
      )
      .subscribe({
        next: (me) => this.navigateAfterAcceptance(me.organizations.length > 0),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected roleLabel(role: string): string {
    return role
      .replace('Client', '')
      .replace('Organization', '')
      .replace(/([A-Z])/g, ' $1')
      .trim();
  }

  protected statusLabel(status: string): string {
    const labels: Record<string, string> = {
      Expired: 'vencida',
      Accepted: 'aceptada',
      Revoked: 'revocada',
    };
    return labels[status] ?? status.toLowerCase();
  }

  private navigateAfterAcceptance(hasOrganization: boolean): void {
    this.toast.success('Invitación aceptada.');
    void this.router.navigate([hasOrganization ? '/app/dashboard' : '/portal/events']);
  }
}

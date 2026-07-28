import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ToastService } from '../../core/ui/toast.service';
import { OrganizationType } from '../../core/models/api.models';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="auth-page auth-page--register">
      <section class="auth-story">
        <a class="brand brand--light" routerLink="/auth/login">
          <span class="brand__mark">P</span>
          <strong>Plannyt</strong>
        </a>
        <div>
          <span class="eyebrow eyebrow--light">Tu nuevo centro de operación</span>
          <h1>Haz espacio para<br />lo que sí importa.</h1>
          <p>
            Crea tu organización y empieza a acompañar cada evento desde una visión clara y
            compartida.
          </p>
        </div>
        <ul class="story-checks">
          <li>Un espacio privado para tu organización</li>
          <li>Accesos seguros para equipo y clientes</li>
          <li>Información compartida bajo tu control</li>
        </ul>
      </section>

      <section class="auth-panel">
        <div class="auth-card auth-card--wide">
          <span class="eyebrow">Comienza hoy</span>
          <h2>Crea tu espacio en Plannyt</h2>
          <p class="muted">Toma menos de dos minutos.</p>

          <form [formGroup]="form" (ngSubmit)="submit()" class="form-grid">
            <label>
              Nombre
              <input formControlName="firstName" autocomplete="given-name" />
            </label>
            <label>
              Apellido
              <input formControlName="lastName" autocomplete="family-name" />
            </label>
            <label class="span-2">
              Correo electrónico
              <input type="email" formControlName="email" autocomplete="email" />
            </label>
            <label class="span-2">
              Contraseña
              <input type="password" formControlName="password" autocomplete="new-password" />
              <small>Usa al menos 12 caracteres.</small>
            </label>
            <label class="span-2">
              Nombre de tu organización
              <input formControlName="organizationName" />
            </label>
            <label>
              Tipo
              <select formControlName="organizationType">
                <option value="IndependentPlanner">Planner independiente</option>
                <option value="Agency">Agencia</option>
              </select>
            </label>
            <label>
              Zona horaria
              <input formControlName="timeZone" />
            </label>
            <label>
              País
              <input formControlName="countryCode" maxlength="2" />
            </label>
            <label>
              Moneda
              <input formControlName="currencyCode" maxlength="3" />
            </label>
            <button
              class="btn btn--primary btn--large span-2"
              type="submit"
              [disabled]="form.invalid || loading()"
            >
              {{ loading() ? 'Creando tu espacio…' : 'Crear organización' }}
            </button>
          </form>

          <p class="auth-switch">
            ¿Ya tienes cuenta?
            <a routerLink="/auth/login">Inicia sesión</a>
          </p>
        </div>
      </section>
    </main>
  `,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  protected readonly loading = signal(false);
  protected readonly form = new FormGroup({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(12), Validators.maxLength(128)],
    }),
    organizationName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(160)],
    }),
    organizationType: new FormControl<OrganizationType>('IndependentPlanner', {
      nonNullable: true,
    }),
    timeZone: new FormControl(
      Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Matamoros',
      { nonNullable: true, validators: [Validators.required] },
    ),
    countryCode: new FormControl('MX', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2)],
    }),
    currencyCode: new FormControl('MXN', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(3)],
    }),
  });

  protected submit(): void {
    if (this.form.invalid || this.loading()) {
      return;
    }

    this.loading.set(true);
    const value = this.form.getRawValue();
    this.auth
      .registerPlanner({
        ...value,
        countryCode: value.countryCode.toUpperCase(),
        currencyCode: value.currencyCode.toUpperCase(),
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => void this.router.navigate(['/app/dashboard']),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }
}

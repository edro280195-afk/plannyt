import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="auth-page">
      <section class="auth-story">
        <a class="brand brand--light" routerLink="/auth/login">
          <span class="brand__mark">P</span>
          <strong>Plannyt</strong>
        </a>
        <div>
          <span class="eyebrow eyebrow--light">Planea con claridad</span>
          <h1>Menos pendientes.<br />Más momentos memorables.</h1>
          <p>
            Clientes, eventos y decisiones compartidas en un solo lugar, sin perder el toque humano.
          </p>
        </div>
        <blockquote>“La calma también puede formar parte de la operación.”</blockquote>
      </section>

      <section class="auth-panel">
        <div class="auth-card">
          <span class="eyebrow">Bienvenida de vuelta</span>
          <h2>Inicia sesión</h2>
          <p class="muted">Retoma la planeación justo donde la dejaste.</p>

          <form [formGroup]="form" (ngSubmit)="submit()" class="form-stack">
            <label>
              Correo electrónico
              <input
                type="email"
                formControlName="email"
                autocomplete="email"
                placeholder="mariana@armonia.mx"
              />
            </label>
            <label>
              Contraseña
              <input
                type="password"
                formControlName="password"
                autocomplete="current-password"
                placeholder="Tu contraseña"
              />
            </label>
            <label class="checkbox-row">
              <input type="checkbox" formControlName="isPersistent" />
              <span>Mantener mi sesión iniciada</span>
            </label>
            <button
              class="btn btn--primary btn--large btn--full"
              type="submit"
              [disabled]="form.invalid || loading()"
            >
              {{ loading() ? 'Ingresando…' : 'Entrar a Plannyt' }}
            </button>
          </form>

          <p class="auth-switch">
            ¿Aún no tienes cuenta?
            <a routerLink="/auth/register">Crea tu espacio</a>
          </p>
        </div>
      </section>
    </main>
  `,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  protected readonly loading = signal(false);
  protected readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    isPersistent: new FormControl(true, { nonNullable: true }),
  });

  protected submit(): void {
    if (this.form.invalid || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.auth
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (me) => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
          const destination =
            returnUrl ?? (me.organizations.length > 0 ? '/app/dashboard' : '/portal/events');
          void this.router.navigateByUrl(destination);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }
}

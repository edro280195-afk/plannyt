import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, switchMap } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { OrganizationType } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-settings-page',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page page--narrow">
      <header class="page-header">
        <div>
          <span class="eyebrow">Preferencias</span>
          <h1>Configuración</h1>
          <p>Datos básicos que dan contexto a toda tu operación.</p>
        </div>
      </header>

      <form class="card card--padded form-stack" [formGroup]="form" (ngSubmit)="save()">
        <div class="section-heading">
          <div>
            <span class="eyebrow">Organización</span>
            <h2>Información general</h2>
          </div>
        </div>
        <div class="form-grid">
          <label class="span-2">
            Nombre
            <input formControlName="name" />
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
            <input maxlength="2" formControlName="countryCode" />
          </label>
          <label>
            Moneda
            <input maxlength="3" formControlName="currencyCode" />
          </label>
        </div>
        <div class="form-actions">
          @if (organization.hasPermission('organization.update')) {
            <button class="btn btn--primary" type="submit" [disabled]="form.invalid || saving()">
              {{ saving() ? 'Guardando…' : 'Guardar cambios' }}
            </button>
          }
        </div>
      </form>

      <section class="card card--padded section-gap danger-zone">
        <span class="eyebrow">Seguridad</span>
        <h2>Cerrar todas las sesiones</h2>
        <p>Revoca inmediatamente todas tus sesiones activas, incluida esta.</p>
        <button class="btn btn--danger-quiet" type="button" (click)="logoutAll()">
          Cerrar todas mis sesiones
        </button>
      </section>
    </div>
  `,
})
export class SettingsPage {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly saving = signal(false);
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    organizationType: new FormControl<OrganizationType>('IndependentPlanner', {
      nonNullable: true,
    }),
    timeZone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    countryCode: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    currencyCode: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  constructor() {
    this.api
      .getOrganization(this.organization.requireOrganizationId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (organization) => this.form.patchValue(organization),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.api
      .updateOrganization(this.organization.requireOrganizationId(), {
        ...value,
        countryCode: value.countryCode.toUpperCase(),
        currencyCode: value.currencyCode.toUpperCase(),
      })
      .pipe(
        switchMap(() => this.auth.reloadMe()),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: () => {
          this.toast.success('Organización actualizada.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected logoutAll(): void {
    if (!window.confirm('¿Cerrar todas tus sesiones activas?')) {
      return;
    }

    this.auth.logoutAll().subscribe({
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }
}

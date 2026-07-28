import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { EventDetailsRequest } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-event-editor-page',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page page--narrow">
      <a class="back-link" [routerLink]="eventId ? ['/app/events', eventId] : '/app/events'">
        ← Volver
      </a>
      <header class="page-header">
        <div>
          <span class="eyebrow">{{ eventId ? 'Editar evento' : 'Nuevo evento' }}</span>
          <h1>{{ eventId ? 'Actualiza los detalles' : 'Dale forma al próximo evento' }}</h1>
          <p>Estos son los datos administrativos y compartibles del evento.</p>
        </div>
      </header>

      <form class="card card--padded form-stack" [formGroup]="form" (ngSubmit)="save()">
        <div class="form-grid">
          <label class="span-2">
            Nombre del evento
            <input formControlName="name" placeholder="Ana & Carlos" />
          </label>
          <label>
            Tipo
            <input formControlName="eventType" placeholder="Boda" />
          </label>
          <label>
            Invitados estimados
            <input type="number" min="0" formControlName="estimatedGuestCount" />
          </label>
          <label>
            Inicio
            <input type="datetime-local" formControlName="startDateTime" />
          </label>
          <label>
            Fin
            <input type="datetime-local" formControlName="endDateTime" />
          </label>
          <label>
            Ciudad
            <input formControlName="city" />
          </label>
          <label>
            País
            <input formControlName="countryCode" maxlength="2" />
          </label>
          <label class="span-2">
            Zona horaria IANA
            <input formControlName="timeZone" />
          </label>
          <label class="span-2">
            Descripción compartida
            <textarea
              formControlName="sharedDescription"
              rows="5"
              placeholder="Una breve descripción que el cliente sí podrá consultar."
            ></textarea>
          </label>
        </div>
        <div class="info-callout">
          <span aria-hidden="true">◌</span>
          <p>
            Esta descripción es visible en el portal. Los datos administrativos permanecen
            separados.
          </p>
        </div>
        <div class="form-actions">
          <a class="btn btn--quiet" routerLink="/app/events">Cancelar</a>
          <button class="btn btn--primary" type="submit" [disabled]="form.invalid || saving()">
            {{ saving() ? 'Guardando…' : 'Guardar evento' }}
          </button>
        </div>
      </form>
    </div>
  `,
})
export class EventEditorPage {
  private readonly api = inject(ApiService);
  private readonly organization = inject(OrganizationContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly eventId = this.route.snapshot.paramMap.get('id');
  protected readonly saving = signal(false);
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    eventType: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(80)],
    }),
    startDateTime: new FormControl(this.defaultLocalDate(30), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    endDateTime: new FormControl(this.defaultLocalDate(30, 6), {
      nonNullable: true,
    }),
    timeZone: new FormControl(
      Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Matamoros',
      { nonNullable: true, validators: [Validators.required] },
    ),
    city: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(120)],
    }),
    countryCode: new FormControl('MX', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    sharedDescription: new FormControl('', { nonNullable: true }),
    estimatedGuestCount: new FormControl<number | null>(null),
  });

  constructor() {
    if (this.eventId) {
      this.api
        .getEvent(this.organization.requireOrganizationId(), this.eventId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (event) =>
            this.form.patchValue({
              name: event.name,
              eventType: event.eventType,
              startDateTime: this.toLocalDate(event.startDateTime),
              endDateTime: event.endDateTime ? this.toLocalDate(event.endDateTime) : '',
              timeZone: event.timeZone,
              city: event.city,
              countryCode: event.countryCode,
              sharedDescription: event.sharedDescription ?? '',
              estimatedGuestCount: event.estimatedGuestCount,
            }),
          error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
        });
    }
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    const organizationId = this.organization.requireOrganizationId();
    const request = this.buildRequest();
    const operation = this.eventId
      ? this.api.updateEvent(organizationId, this.eventId, request)
      : this.api.createEvent(organizationId, request);
    this.saving.set(true);
    operation
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (event) => {
          this.toast.success('Evento guardado correctamente.');
          void this.router.navigate(['/app/events', event.id]);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private buildRequest(): EventDetailsRequest {
    const value = this.form.getRawValue();
    return {
      name: value.name,
      eventType: value.eventType,
      startDateTime: new Date(value.startDateTime).toISOString(),
      endDateTime: value.endDateTime ? new Date(value.endDateTime).toISOString() : null,
      timeZone: value.timeZone,
      city: value.city,
      countryCode: value.countryCode.toUpperCase(),
      sharedDescription: value.sharedDescription || null,
      estimatedGuestCount: value.estimatedGuestCount,
    };
  }

  private defaultLocalDate(daysAhead: number, hoursAhead = 0): string {
    const date = new Date();
    date.setDate(date.getDate() + daysAhead);
    date.setHours(12 + hoursAhead, 0, 0, 0);
    return this.toLocalDate(date.toISOString());
  }

  private toLocalDate(value: string): string {
    const date = new Date(value);
    const offset = date.getTimezoneOffset() * 60_000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
  }
}

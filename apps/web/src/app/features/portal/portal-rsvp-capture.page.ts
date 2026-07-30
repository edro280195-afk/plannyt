import { Component, ChangeDetectionStrategy, DestroyRef, signal, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/api/api.service';
import { IdempotencyAttempt } from '../../core/api/idempotency-attempt';
import { ToastService } from '../../core/ui/toast.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import type { ManualRsvpRequest, RsvpSubmissionResponse } from '../../core/models/api.models';

@Component({
  selector: 'app-portal-rsvp-capture-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-page">
      <a class="back-link" [routerLink]="['/portal/events', eventId, 'rsvp']">← Volver al RSVP</a>
      <h1>Registrar respuesta RSVP</h1>
      <p class="help-text">
        Use esta función para registrar respuestas recibidas por teléfono, WhatsApp u otro medio.
      </p>
      @if (formLoading()) {
        <p class="load-state" role="status">Cargando formulario RSVP…</p>
      } @else if (formLoadError(); as message) {
        <div class="load-error" role="alert">
          <span>{{ message }}</span>
          <button type="button" class="retry-button" (click)="loadFormVersion()">Reintentar</button>
        </div>
      }
      <form [formGroup]="form" (ngSubmit)="submit()" class="capture-form">
        <div class="form-group">
          <label for="rsvp-group-id">Grupo</label>
          <input id="rsvp-group-id" formControlName="groupId" placeholder="ID del grupo" />
        </div>
        <div class="form-group">
          <label for="rsvp-overall-status">Estado general</label>
          <select id="rsvp-overall-status" formControlName="overallStatus">
            <option value="Confirmed">Confirmado</option>
            <option value="Declined">Rechazado</option>
            <option value="Tentative">Tentativo</option>
            <option value="Mixed">Mixto</option>
          </select>
        </div>
        <div class="form-group">
          <label for="rsvp-contact-name">Nombre del contacto</label>
          <input id="rsvp-contact-name" formControlName="contactName" />
        </div>
        <div class="form-group">
          <label for="rsvp-expected-revision">Revisión observada</label>
          <input
            id="rsvp-expected-revision"
            type="number"
            min="0"
            formControlName="expectedRevision"
          />
          <small>Usa 0 para la primera captura.</small>
        </div>
        <div class="form-group">
          <label for="rsvp-source">Fuente</label>
          <select id="rsvp-source" formControlName="source">
            <option value="PlannerManual">Llamada telefónica</option>
            <option value="Imported">WhatsApp / mensaje</option>
            <option value="SupportCorrection">Corrección de soporte</option>
          </select>
        </div>
        <div class="form-group">
          <label for="rsvp-reason">Motivo / nota</label>
          <textarea id="rsvp-reason" formControlName="reason" rows="3"></textarea>
        </div>
        <button
          type="submit"
          [disabled]="form.invalid || submitting() || formLoading() || !formVersionId()"
        >
          Registrar respuesta
        </button>
      </form>
      @if (result(); as r) {
        <div class="result">
          <p>Respuesta registrada: {{ r.confirmationCode }}</p>
          <p>{{ r.guests.length }} invitados · {{ r.overallStatus }}</p>
        </div>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        padding: 24px;
        max-width: 600px;
        margin: 0 auto;
      }
      .back-link {
        display: inline-block;
        margin-bottom: 16px;
        color: #1a73e8;
        text-decoration: none;
        font-size: 14px;
      }
      .help-text {
        color: #666;
        margin-bottom: 24px;
      }
      .capture-form {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
      .form-group {
        display: flex;
        flex-direction: column;
      }
      .form-group label {
        margin-bottom: 4px;
        font-weight: 600;
      }
      .form-group input,
      .form-group select,
      .form-group textarea {
        padding: 10px;
        border: 1px solid #ddd;
        border-radius: 6px;
      }
      button {
        padding: 12px 24px;
        background: #1a73e8;
        color: white;
        border: none;
        border-radius: 6px;
        cursor: pointer;
      }
      button:disabled {
        background: #a0c4f1;
      }
      .load-state {
        color: #475569;
      }
      .load-error {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        padding: 12px;
        border: 1px solid #b91c1c;
        border-radius: 6px;
        color: #7f1d1d;
        background: #fef2f2;
      }
      .retry-button {
        flex: none;
        padding: 8px 12px;
      }
      .result {
        margin-top: 24px;
        padding: 16px;
        background: #e8f5e9;
        border-radius: 8px;
      }
    `,
  ],
})
export class PortalRsvpCapturePage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly eventId =
    this.route.snapshot.paramMap.get('id') ??
    (() => {
      throw new Error('La ruta requiere un evento.');
    })();

  protected readonly form = new FormGroup({
    groupId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    overallStatus: new FormControl<'Confirmed' | 'Declined' | 'Tentative' | 'Mixed'>('Confirmed', {
      nonNullable: true,
    }),
    contactName: new FormControl('', { nonNullable: true }),
    expectedRevision: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
    source: new FormControl<'PlannerManual' | 'Imported' | 'SupportCorrection'>('PlannerManual', {
      nonNullable: true,
    }),
    reason: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected readonly result = signal<RsvpSubmissionResponse | null>(null);
  protected readonly submitting = signal(false);
  protected readonly formLoading = signal(true);
  protected readonly formLoadError = signal<string | null>(null);
  protected readonly formVersionId = signal<string | null>(null);
  private readonly idempotencyAttempt = new IdempotencyAttempt();

  constructor() {
    this.loadFormVersion();
  }

  protected loadFormVersion(): void {
    this.formLoading.set(true);
    this.formLoadError.set(null);
    this.api
      .getPortalRsvpForm(this.eventId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (form) => {
          this.formVersionId.set(form.id);
          this.formLoading.set(false);
        },
        error: (error) => {
          const message = getApiErrorMessage(error);
          this.formVersionId.set(null);
          this.formLoading.set(false);
          this.formLoadError.set(message);
          this.toast.error(message);
        },
      });
  }

  protected submit(): void {
    const formVersionId = this.formVersionId();
    if (this.form.invalid || this.submitting() || !formVersionId) {
      return;
    }
    this.submitting.set(true);
    const eventId = this.route.snapshot.params['id'] as string;
    const f = this.form.getRawValue();
    const request: ManualRsvpRequest = {
      source: f.source,
      reason: f.reason,
      submission: {
        rsvpFormVersionId: formVersionId,
        expectedRevision: f.expectedRevision,
        overallStatus: f.overallStatus,
        contactName: f.contactName || null,
        contactEmail: null,
        contactPhone: null,
        guests: [],
        answers: [],
        consentSnapshot: null,
      },
    };
    const payload = JSON.stringify(request);
    const idempotencyKey = this.idempotencyAttempt.keyFor(payload);
    this.api
      .manualPortalRsvpCapture(eventId, f.groupId, request, idempotencyKey)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => {
          this.idempotencyAttempt.complete();
          this.result.set(r);
          this.submitting.set(false);
          this.toast.success('Respuesta registrada.');
        },
        error: (err) => {
          this.toast.error(getApiErrorMessage(err));
          this.submitting.set(false);
        },
      });
  }
}

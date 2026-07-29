import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { ToastService } from '../../core/ui/toast.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { RsvpSettingsRequest, RsvpSettingsResponse } from '../../core/models/api.models';

@Component({
  selector: 'app-rsvp-settings-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page page--narrow">
      <a class="back-link" [routerLink]="['/app/events', eventId, 'rsvp']">
        &larr; Volver a RSVP
      </a>
      <header class="page-header">
        <div>
          <span class="eyebrow">Configuración</span>
          <h1>RSVP del evento</h1>
          <p>Define ventanas de respuesta, reglas, mensajes y textos legales.</p>
        </div>
        <div class="header-actions">
          @if (loadedSettings(); as s) {
            <span class="badge badge--{{ s.status.toLowerCase() }}">{{ statusLabel(s.status) }}</span>
          }
        </div>
      </header>

      <form class="card card--padded form-stack" [formGroup]="form" (ngSubmit)="save()">
        <h2>Ventana de respuesta</h2>
        <div class="form-grid">
          <label>
            Apertura (opcional)
            <input type="datetime-local" formControlName="opensAt" />
          </label>
          <label>
            Cierre (opcional)
            <input type="datetime-local" formControlName="closesAt" />
          </label>
          <label>
            Zona horaria
            <select formControlName="timeZone">
              <option value="America/Mexico_City">Ciudad de México (UTC-6)</option>
              <option value="America/Matamoros">Matamoros (UTC-6)</option>
              <option value="America/Monterrey">Monterrey (UTC-6)</option>
              <option value="America/Cancun">Cancún (UTC-5)</option>
              <option value="America/Argentina/Buenos_Aires">Buenos Aires (UTC-3)</option>
              <option value="America/Bogota">Bogotá (UTC-5)</option>
              <option value="America/Lima">Lima (UTC-5)</option>
              <option value="America/Santiago">Santiago (UTC-4)</option>
              <option value="Europe/Madrid">Madrid (UTC+1)</option>
              <option value="US/Eastern">Este EE.UU. (UTC-5)</option>
              <option value="US/Central">Centro EE.UU. (UTC-6)</option>
            </select>
          </label>
          <label>
            Cierre de cambios (opcional)
            <input type="datetime-local" formControlName="changesCloseAt" />
          </label>
        </div>

        <h2>Reglas</h2>
        <div class="form-grid">
          <label class="check-line">
            <input type="checkbox" formControlName="allowChangesAfterSubmission" />
            <span>Permitir cambios después de enviar</span>
          </label>
          <label class="check-line">
            <input type="checkbox" formControlName="allowTentativeResponse" />
            <span>Permitir confirmación tentativa</span>
          </label>
          <label class="check-line">
            <input type="checkbox" formControlName="allowGroupDecline" />
            <span>Permitir que el grupo decline completo</span>
          </label>
          <label class="check-line">
            <input type="checkbox" formControlName="requireResponseForEveryNamedGuest" />
            <span>Exigir respuesta individual por invitado</span>
          </label>
          <label class="check-line">
            <input type="checkbox" formControlName="requireCompanionNames" />
            <span>Obligar nombre de acompañantes</span>
          </label>
          <label class="check-line">
            <input type="checkbox" formControlName="allowContactInformationUpdate" />
            <span>Permitir actualizar datos de contacto</span>
          </label>
          <label class="check-line">
            <input type="checkbox" formControlName="showAttendanceSummaryAfterSubmission" />
            <span>Mostrar resumen tras enviar respuesta</span>
          </label>
        </div>

        <h2>Mensajes</h2>
        <div class="form-grid">
          <label>
            Título de confirmación
            <input formControlName="confirmationTitle" placeholder="¡Gracias por confirmar!" />
          </label>
          <label>
            Mensaje de confirmación
            <textarea rows="3" formControlName="confirmationMessage" placeholder="Nos alegra que nos acompañes en este día especial."></textarea>
          </label>
          <label>
            Mensaje al declinar
            <textarea rows="3" formControlName="declineMessage" placeholder="Lamentamos que no puedas asistir."></textarea>
          </label>
          <label>
            Mensaje de RSVP cerrado
            <textarea rows="3" formControlName="closedMessage" placeholder="El periodo de confirmación ha finalizado."></textarea>
          </label>
        </div>

        <h2>Textos legales</h2>
        <div class="form-grid">
          <label class="span-2">
            Aviso de privacidad
            <textarea rows="4" formControlName="privacyNotice" placeholder="Los datos recabados serán utilizados exclusivamente para la organización del evento..."></textarea>
          </label>
          <label class="span-2">
            Texto de consentimiento para datos sensibles
            <textarea rows="3" formControlName="sensitiveDataConsentText" placeholder="Autorizo el tratamiento de información sobre alergias, restricciones alimentarias..."></textarea>
          </label>
        </div>

        <div class="section-footer">
          <div class="info-callout">
            <span aria-hidden="true">◌</span>
            <p>La configuración se guarda como borrador. Publícala cuando esté lista para recibir respuestas.</p>
          </div>
        </div>

        <div class="form-actions">
          <a class="btn btn--quiet" [routerLink]="['/app/events', eventId, 'rsvp']">Cancelar</a>
          <button class="btn btn--secondary" type="submit" [disabled]="form.invalid || saving()">
            {{ saving() ? 'Guardando…' : 'Guardar borrador' }}
          </button>
          @if (loadedSettings(); as s) {
            @if (s.status === 'Draft' || s.status === 'Ready') {
              <button
                class="btn btn--primary"
                type="button"
                [disabled]="publishing()"
                (click)="publish()"
              >
                {{ publishing() ? 'Publicando…' : 'Publicar configuración' }}
              </button>
            }
          }
        </div>
      </form>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .page--narrow { max-width: 720px; margin: 0 auto; padding: 24px; }
    .back-link { color: #1a73e8; text-decoration: none; display: inline-block; margin-bottom: 16px; }
    .page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; flex-wrap: wrap; gap: 12px; }
    .eyebrow { font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #888; }
    h1 { margin: 4px 0 4px; font-size: 24px; }
    h2 { font-size: 16px; margin: 24px 0 12px; padding-bottom: 6px; border-bottom: 1px solid #eee; }
    .header-actions { display: flex; gap: 8px; }
    .badge { padding: 2px 8px; border-radius: 12px; font-size: 12px; background: #e8f0fe; color: #1a73e8; }
    .badge--draft { background: #f1f3f4; color: #666; }
    .badge--ready { background: #e8f5e9; color: #1e8e3e; }
    .badge--open { background: #e8f0fe; color: #1a73e8; }
    .badge--closed, .badge--suspended { background: #fce8e6; color: #d93025; }
    .badge--archived { background: #f3e5f5; color: #7b1fa2; }
    .card { background: white; border: 1px solid #eee; border-radius: 8px; }
    .card--padded { padding: 24px; }
    .form-stack { display: flex; flex-direction: column; gap: 0; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .span-2 { grid-column: 1 / -1; }
    label { display: flex; flex-direction: column; gap: 4px; font-size: 13px; color: #333; }
    input, select, textarea { padding: 8px; border: 1px solid #ddd; border-radius: 6px; font-size: 14px; }
    .check-line { flex-direction: row !important; align-items: center; gap: 8px; }
    .section-footer { margin-top: 12px; }
    .info-callout { display: flex; gap: 8px; align-items: flex-start; background: #e8f0fe; padding: 12px; border-radius: 8px; font-size: 13px; color: #174ea6; }
    .form-actions { display: flex; gap: 8px; align-items: center; margin-top: 24px; }
    .btn { padding: 8px 16px; border: 1px solid #ddd; border-radius: 6px; background: white; cursor: pointer; font-size: 14px; }
    .btn--primary { background: #1a73e8; color: white; border-color: #1a73e8; }
    .btn--secondary { background: #f1f3f4; color: #333; border-color: #ddd; }
    .btn--quiet { background: none; border: none; color: #1a73e8; }
    .btn:disabled { opacity: 0.6; cursor: not-allowed; }
  `],
})
export class RsvpSettingsPage {
  private readonly api = inject(ApiService);
  private readonly organization = inject(OrganizationContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly eventId = this.route.snapshot.paramMap.get('id');
  protected readonly loadedSettings = signal<RsvpSettingsResponse | null>(null);
  protected readonly saving = signal(false);
  protected readonly publishing = signal(false);

  protected readonly form = new FormGroup({
    opensAt: new FormControl<string | null>(null),
    closesAt: new FormControl<string | null>(null),
    timeZone: new FormControl(
      Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Matamoros',
      { nonNullable: true, validators: [Validators.required] },
    ),
    changesCloseAt: new FormControl<string | null>(null),
    allowChangesAfterSubmission: new FormControl(true, { nonNullable: true }),
    allowTentativeResponse: new FormControl(false, { nonNullable: true }),
    allowGroupDecline: new FormControl(false, { nonNullable: true }),
    requireResponseForEveryNamedGuest: new FormControl(false, { nonNullable: true }),
    requireCompanionNames: new FormControl(false, { nonNullable: true }),
    allowContactInformationUpdate: new FormControl(true, { nonNullable: true }),
    showAttendanceSummaryAfterSubmission: new FormControl(true, { nonNullable: true }),
    confirmationTitle: new FormControl<string | null>(null),
    confirmationMessage: new FormControl<string | null>(null),
    declineMessage: new FormControl<string | null>(null),
    closedMessage: new FormControl<string | null>(null),
    privacyNotice: new FormControl<string | null>(null),
    sensitiveDataConsentText: new FormControl<string | null>(null),
  });

  constructor() {
    if (this.eventId) {
      this.load();
    }
  }

  private load(): void {
    this.api
      .getRsvpSettings(this.organization.requireOrganizationId(), this.eventId!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (settings) => {
          this.loadedSettings.set(settings);
          this.form.patchValue({
            opensAt: settings.opensAt ? this.toLocalDate(settings.opensAt) : null,
            closesAt: settings.closesAt ? this.toLocalDate(settings.closesAt) : null,
            timeZone: settings.timeZone,
            changesCloseAt: settings.changesCloseAt ? this.toLocalDate(settings.changesCloseAt) : null,
            allowChangesAfterSubmission: settings.allowChangesAfterSubmission,
            allowTentativeResponse: settings.allowTentativeResponse,
            allowGroupDecline: settings.allowGroupDecline,
            requireResponseForEveryNamedGuest: settings.requireResponseForEveryNamedGuest,
            requireCompanionNames: settings.requireCompanionNames,
            allowContactInformationUpdate: settings.allowContactInformationUpdate,
            showAttendanceSummaryAfterSubmission: settings.showAttendanceSummaryAfterSubmission,
            confirmationTitle: settings.confirmationTitle,
            confirmationMessage: settings.confirmationMessage,
            declineMessage: settings.declineMessage,
            closedMessage: settings.closedMessage,
            privacyNotice: settings.privacyNotice,
            sensitiveDataConsentText: settings.sensitiveDataConsentText,
          });
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) return;
    const organizationId = this.organization.requireOrganizationId();
    const request = this.buildRequest();
    this.saving.set(true);
    this.api
      .updateRsvpSettings(organizationId, this.eventId!, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (settings) => {
          this.loadedSettings.set(settings);
          this.toast.success('Configuración guardada.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected publish(): void {
    const organizationId = this.organization.requireOrganizationId();
    this.publishing.set(true);
    this.api
      .publishRsvpSettings(organizationId, this.eventId!)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.publishing.set(false)),
      )
      .subscribe({
        next: (settings) => {
          this.loadedSettings.set(settings);
          this.toast.success('Configuración publicada.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private buildRequest(): RsvpSettingsRequest {
    const value = this.form.getRawValue();
    return {
      opensAt: value.opensAt ? new Date(value.opensAt).toISOString() : null,
      closesAt: value.closesAt ? new Date(value.closesAt).toISOString() : null,
      timeZone: value.timeZone,
      allowChangesAfterSubmission: value.allowChangesAfterSubmission,
      changesCloseAt: value.changesCloseAt ? new Date(value.changesCloseAt).toISOString() : null,
      allowTentativeResponse: value.allowTentativeResponse,
      allowGroupDecline: value.allowGroupDecline,
      requireResponseForEveryNamedGuest: value.requireResponseForEveryNamedGuest,
      requireCompanionNames: value.requireCompanionNames,
      allowContactInformationUpdate: value.allowContactInformationUpdate,
      showAttendanceSummaryAfterSubmission: value.showAttendanceSummaryAfterSubmission,
      confirmationTitle: value.confirmationTitle || null,
      confirmationMessage: value.confirmationMessage || null,
      declineMessage: value.declineMessage || null,
      closedMessage: value.closedMessage || null,
      privacyNotice: value.privacyNotice || null,
      sensitiveDataConsentText: value.sensitiveDataConsentText || null,
    };
  }

  protected statusLabel(status: string): string {
    return (
      (
        {
          Draft: 'Borrador',
          Ready: 'Listo',
          Open: 'Abierto',
          Closed: 'Cerrado',
          Suspended: 'Suspendido',
          Archived: 'Archivado',
        } as Record<string, string>
      )[status] ?? status
    );
  }

  private toLocalDate(value: string): string {
    const date = new Date(value);
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
  }
}

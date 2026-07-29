import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { ToastService } from '../../core/ui/toast.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  RsvpDashboardResponse,
  RsvpSettingsResponse,
  SensitiveGuestDataResponse,
  SensitiveQuestionAnswerResponse,
} from '../../core/models/api.models';

@Component({
  selector: 'app-rsvp-dashboard-page',
  standalone: true,
  imports: [RouterLink, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-header">
      <a [routerLink]="['/app/events', eventId()]" class="back-link">&larr; Evento</a>
      <h1>RSVP</h1>
      <div class="header-actions">
        @if (settings(); as s) {
          @switch (s.status) {
            @case ('Draft') { <button class="btn" (click)="publishSettings()">Publicar configuración</button> }
            @case ('Ready') { <button class="btn btn-primary" (click)="openRsvp()">Abrir RSVP</button> }
            @case ('Open') { <button class="btn btn-warning" (click)="closeRsvp()">Cerrar RSVP</button> }
            @case ('Closed') { <button class="btn" (click)="openRsvp()">Reabrir RSVP</button> }
            @case ('Suspended') { <span class="badge">Suspendido</span> }
            @case ('Archived') { <span class="badge">Archivado</span> }
          }
        }
        <a [routerLink]="['settings']" class="btn-link">Configuración</a>
        <a [routerLink]="['form']" class="btn-link">Formulario</a>
        @if (canViewSensitiveData()) {
          <button class="btn" type="button" (click)="loadSensitiveData()">
            Ver datos sensibles
          </button>
        }
        @if (canExportSensitiveData()) {
          <button class="btn" type="button" (click)="exportSensitiveData()">
            Exportar datos sensibles
          </button>
        }
      </div>

      @if (sensitiveData(); as records) {
        <section class="sensitive-panel" aria-label="Datos sensibles de invitados">
          <div class="sensitive-panel__header">
            <h2>Datos sensibles</h2>
            <button
              class="btn"
              type="button"
              (click)="closeSensitiveData()"
            >Cerrar</button>
          </div>
          @if (records.length === 0) {
            <p>No hay datos sensibles registrados.</p>
          } @else {
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Invitado</th>
                    <th>Alergias</th>
                    <th>Restricciones</th>
                    <th>Accesibilidad</th>
                    <th>Notas</th>
                    <th>Consentimiento</th>
                  </tr>
                </thead>
                <tbody>
                  @for (record of records; track record.eventGuestId) {
                    <tr>
                      <td>{{ record.displayName }}</td>
                      <td>{{ record.allergies || '—' }}</td>
                      <td>{{ record.dietaryRestrictions || '—' }}</td>
                      <td>{{ record.accessibilityRequirements || '—' }}</td>
                      <td>{{ record.additionalNotes || '—' }}</td>
                      <td>{{ record.consentGrantedAt ? (record.consentGrantedAt | date:'short') : 'No otorgado' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
          @if (sensitiveQuestionAnswers(); as questionAnswers) {
            <h3>Respuestas sensibles del formulario</h3>
            @if (questionAnswers.length === 0) {
              <p>No hay respuestas sensibles en la revisión vigente.</p>
            } @else {
              <div class="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Pregunta</th>
                      <th>Invitado</th>
                      <th>Respuesta</th>
                      <th>Revisión</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (
                      answer of questionAnswers;
                      track answer.submissionId + answer.questionId
                    ) {
                      <tr>
                        <td>{{ answer.questionLabel }}</td>
                        <td>{{ answer.guestDisplayName || 'Grupo' }}</td>
                        <td>{{ answer.displayValue || answer.answerValue }}</td>
                        <td>{{ answer.revisionNumber }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          }
        </section>
      }
    </div>

    @if (loading()) {
      <p>Cargando dashboard...</p>
    } @else if (dash(); as d) {
      <div class="stats-grid">
        <div class="stat-card">
          <span class="stat-value">{{ d.guestsConfirmed }}</span>
          <span class="stat-label">Confirmados</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ d.guestsNotAttending }}</span>
          <span class="stat-label">No asistirán</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ d.guestsPending }}</span>
          <span class="stat-label">Pendientes</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ d.guestsTentative }}</span>
          <span class="stat-label">Tentativos</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ d.guestsConfirmed + d.guestsNotAttending + d.guestsTentative }}</span>
          <span class="stat-label">de {{ d.totalGuestsGranted }}</span>
        </div>
      </div>

      <div class="groups-list">
        <h2>Grupos ({{ d.groups.length }})</h2>
        @for (g of d.groups; track g.groupId) {
          <div class="group-card">
            <div class="group-card__header">
              <strong>{{ g.groupName }}</strong>
              @if (g.status) {
                <span class="badge badge--{{ g.status.toLowerCase() }}">{{ g.status }}</span>
              }
            </div>
            <div class="group-card__stats">
              <span>{{ g.confirmedCount }} ✓</span>
              <span>{{ g.declinedCount }} ✗</span>
              <span>{{ g.pendingCount }} ?</span>
            </div>
            <div class="group-card__tags">
              @if (g.hasMenuSelection) { <span class="tag">Menú</span> }
              @if (g.hasTransport) { <span class="tag">Transporte</span> }
              @if (g.hasAccommodation) { <span class="tag">Hospedaje</span> }
              @if (canViewSensitiveData() && g.hasSensitiveData) {
                <span class="tag tag--sensitive">Sensible</span>
              }
            </div>
            @if (g.lastResponseAt) {
              <small>Última: {{ g.lastResponseAt | date:'short' }}</small>
            }
          </div>
        }
      </div>
    }
  `,
  styles: [`
    :host { display: block; padding: 24px; }
    .page-header { display: flex; align-items: center; gap: 16px; margin-bottom: 24px; flex-wrap: wrap; }
    .back-link { color: #1a73e8; text-decoration: none; }
    .header-actions { display: flex; gap: 8px; margin-left: auto; flex-wrap: wrap; }
    .btn { padding: 8px 16px; border: 1px solid #ddd; border-radius: 6px; background: white; cursor: pointer; }
    .btn-primary { background: #1a73e8; color: white; border-color: #1a73e8; }
    .btn-warning { background: #e37400; color: white; border-color: #e37400; }
    .btn-link { color: #1a73e8; text-decoration: underline; padding: 8px; }
    .stats-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 12px; margin-bottom: 32px; }
    .stat-card { background: #f8f9fa; padding: 16px; border-radius: 8px; text-align: center; }
    .stat-value { font-size: 28px; font-weight: 700; display: block; }
    .stat-label { font-size: 12px; color: #666; }
    .groups-list { margin-top: 24px; }
    .group-card { background: white; border: 1px solid #eee; border-radius: 8px; padding: 16px; margin-bottom: 12px; }
    .group-card__header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
    .group-card__stats { display: flex; gap: 16px; color: #666; }
    .group-card__tags { display: flex; gap: 4px; margin-top: 8px; }
    .badge { padding: 2px 8px; border-radius: 12px; font-size: 12px; background: #e8f0fe; color: #1a73e8; }
    .badge--declined { background: #fce8e6; color: #d93025; }
    .badge--tentative { background: #fef7e0; color: #e37400; }
    .badge--pending, .badge--incomplete { background: #f1f3f4; color: #666; }
    .tag { padding: 2px 6px; border-radius: 4px; font-size: 11px; background: #e8f5e9; color: #1e8e3e; }
    .tag--sensitive { background: #fce8e6; color: #d93025; }
    .sensitive-panel { margin: 0 0 24px; padding: 16px; border: 1px solid #f3c7c3; border-radius: 8px; background: #fffafa; }
    .sensitive-panel__header { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 8px; border-bottom: 1px solid #eee; text-align: left; vertical-align: top; }
    small { color: #999; }
    @media (max-width: 720px) {
      .page-header { align-items: stretch; }
      .header-actions {
        width: 100%;
        margin-left: 0;
        flex-direction: column;
      }
      .header-actions .btn,
      .header-actions .btn-link {
        width: 100%;
        box-sizing: border-box;
        text-align: center;
      }
    }
  `],
})
export class RsvpDashboardPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly org = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly eventId = signal('');
  protected readonly dash = signal<RsvpDashboardResponse | null>(null);
  protected readonly settings = signal<RsvpSettingsResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly sensitiveData = signal<SensitiveGuestDataResponse[] | null>(null);
  protected readonly sensitiveQuestionAnswers =
    signal<SensitiveQuestionAnswerResponse[] | null>(null);
  protected readonly canViewSensitiveData = computed(() =>
    this.org.hasPermission('guest-sensitive-data.view'),
  );
  protected readonly canExportSensitiveData = computed(() =>
    this.org.hasPermission('guest-sensitive-data.export'),
  );

  constructor() {
    this.route.params.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.eventId.set(params['id'] as string);
      this.loadData();
    });
  }

  private loadData(): void {
    const orgId = this.org.requireOrganizationId();
    const eid = this.eventId();
    this.loading.set(true);
    forkJoin({
      dash: this.api.getRsvpDashboard(orgId, eid),
      settings: this.api.getRsvpSettings(orgId, eid),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ dash, settings }) => {
          this.dash.set(dash);
          this.settings.set(settings);
          this.loading.set(false);
        },
        error: (err) => {
          this.toast.error(getApiErrorMessage(err));
          this.loading.set(false);
        },
      });
  }

  protected publishSettings(): void {
    const orgId = this.org.requireOrganizationId();
    this.api
      .publishRsvpSettings(orgId, this.eventId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (s) => {
          this.settings.set(s);
          this.toast.success('Configuración publicada.');
        },
        error: (err) => this.toast.error(getApiErrorMessage(err)),
      });
  }

  protected openRsvp(): void {
    const orgId = this.org.requireOrganizationId();
    this.api
      .openRsvp(orgId, this.eventId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (s) => {
          this.settings.set(s);
          this.toast.success('RSVP abierto.');
        },
        error: (err) => this.toast.error(getApiErrorMessage(err)),
      });
  }

  protected closeRsvp(): void {
    const orgId = this.org.requireOrganizationId();
    this.api
      .closeRsvp(orgId, this.eventId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (s) => {
          this.settings.set(s);
          this.toast.success('RSVP cerrado.');
        },
        error: (err) => this.toast.error(getApiErrorMessage(err)),
      });
  }

  protected loadSensitiveData(): void {
    if (!this.canViewSensitiveData()) return;
    const organizationId = this.org.requireOrganizationId();
    forkJoin({
      guestData: this.api.getRsvpSensitiveData(
        organizationId,
        this.eventId(),
      ),
      questionAnswers: this.api.getRsvpSensitiveQuestionAnswers(
        organizationId,
        this.eventId(),
      ),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ guestData, questionAnswers }) => {
          this.sensitiveData.set(guestData);
          this.sensitiveQuestionAnswers.set(questionAnswers);
        },
        error: (err) => this.toast.error(getApiErrorMessage(err)),
      });
  }

  protected closeSensitiveData(): void {
    this.sensitiveData.set(null);
    this.sensitiveQuestionAnswers.set(null);
  }

  protected exportSensitiveData(): void {
    if (!this.canExportSensitiveData()) return;
    this.api
      .exportRsvpSensitiveData(
        this.org.requireOrganizationId(),
        this.eventId(),
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          anchor.href = url;
          anchor.download = `rsvp-sensitive-${this.eventId()}.csv`;
          anchor.click();
          URL.revokeObjectURL(url);
        },
        error: (err) => this.toast.error(getApiErrorMessage(err)),
      });
  }
}

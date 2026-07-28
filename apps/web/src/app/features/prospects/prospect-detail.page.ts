import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  ClientMatchSuggestion,
  CreateProspectActivityRequest,
  ProspectResponse,
  ProspectStatus,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-prospect-detail-page',
  imports: [FormsModule, RouterLink, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page page--narrow">
      <a class="back-link" routerLink="/app/prospects">← Volver al pipeline</a>
      @if (loading()) {
        <div class="card card--padded"><div class="skeleton skeleton--row"></div></div>
      } @else if (prospect(); as current) {
        <header class="detail-hero card card--padded">
          <div>
            <span class="eyebrow">{{ current.eventTypeInterest ?? 'Prospecto comercial' }}</span>
            <h1>{{ current.displayName }}</h1>
            <p>
              {{ current.email ?? 'Sin correo' }} · {{ current.phone ?? 'Sin teléfono' }}
              @if (current.companyName) {
                · {{ current.companyName }}
              }
            </p>
          </div>
          <span class="status-chip" [attr.data-status]="current.status">
            {{ statusLabel(current.status) }}
          </span>
        </header>

        <div class="detail-grid section-gap">
          <div class="detail-main">
            <section class="card card--padded">
              <div class="section-heading">
                <div>
                  <span class="eyebrow">Siguiente paso</span>
                  <h2>Mover oportunidad</h2>
                </div>
              </div>
              <div class="inline-form">
                <label>
                  Nuevo estado
                  <select [(ngModel)]="nextStatus">
                    <option value="">Selecciona</option>
                    @for (status of allowedStatuses(current.status); track status) {
                      <option [value]="status">{{ statusLabel(status) }}</option>
                    }
                  </select>
                </label>
                @if (nextStatus === 'Lost') {
                  <label class="grow">
                    Motivo de pérdida
                    <input [(ngModel)]="statusReason" maxlength="500" required />
                  </label>
                }
                <button
                  class="btn btn--secondary"
                  type="button"
                  [disabled]="!nextStatus || changingStatus()"
                  (click)="changeStatus()"
                >
                  Actualizar estado
                </button>
              </div>
            </section>

            <section class="card card--padded section-gap">
              <div class="section-heading">
                <div>
                  <span class="eyebrow">Relación</span>
                  <h2>Actividad comercial</h2>
                </div>
              </div>
              <form class="form-stack" (ngSubmit)="addActivity()">
                <div class="form-grid">
                  <label>
                    Tipo
                    <select name="activityType" [(ngModel)]="activity.activityType">
                      <option value="FollowUp">Seguimiento</option>
                      <option value="Call">Llamada</option>
                      <option value="WhatsApp">WhatsApp</option>
                      <option value="Email">Correo</option>
                      <option value="Meeting">Reunión</option>
                      <option value="Note">Nota</option>
                    </select>
                  </label>
                  <label>
                    Programar para
                    <input
                      name="scheduledAt"
                      type="datetime-local"
                      [(ngModel)]="activity.scheduledAt"
                    />
                  </label>
                  <label class="span-2">
                    Asunto
                    <input name="subject" [(ngModel)]="activity.subject" required maxlength="180" />
                  </label>
                  <label class="span-2">
                    Detalle
                    <textarea name="description" [(ngModel)]="activity.description"></textarea>
                  </label>
                </div>
                <div class="form-actions">
                  <button class="btn btn--primary" type="submit" [disabled]="savingActivity()">
                    Registrar actividad
                  </button>
                </div>
              </form>

              <div class="timeline section-gap">
                @for (item of current.activities; track item.id) {
                  <article class="timeline-item">
                    <span class="timeline-item__dot"></span>
                    <div>
                      <strong>{{ activityLabel(item.activityType) }} · {{ item.subject }}</strong>
                      <small>{{ item.createdAt | date: 'dd MMM yyyy, HH:mm' }}</small>
                      @if (item.description) {
                        <p>{{ item.description }}</p>
                      }
                      @if (item.scheduledAt && !item.completedAt) {
                        <button
                          class="btn btn--quiet btn--small"
                          type="button"
                          (click)="completeActivity(item.id)"
                        >
                          Marcar completado
                        </button>
                      }
                    </div>
                  </article>
                } @empty {
                  <div class="empty-state empty-state--compact">
                    <p>Aún no hay seguimientos registrados.</p>
                  </div>
                }
              </div>
            </section>
          </div>

          <aside class="detail-aside">
            <section class="card card--padded">
              <span class="eyebrow">Oportunidad</span>
              <dl class="detail-list">
                <div>
                  <dt>Fecha estimada</dt>
                  <dd>{{ current.estimatedEventDate ?? 'Por definir' }}</dd>
                </div>
                <div>
                  <dt>Invitados</dt>
                  <dd>{{ current.estimatedGuestCount ?? 'Por definir' }}</dd>
                </div>
                <div>
                  <dt>Presupuesto</dt>
                  <dd>{{ current.estimatedBudget ?? 'Por definir' }} {{ current.currencyCode }}</dd>
                </div>
                <div>
                  <dt>Ciudad</dt>
                  <dd>{{ current.city ?? 'Por definir' }}</dd>
                </div>
                <div>
                  <dt>Origen</dt>
                  <dd>{{ current.source ?? 'No registrado' }}</dd>
                </div>
              </dl>
              @if (current.notes) {
                <div class="internal-note">
                  <strong>Nota interna</strong>
                  <p>{{ current.notes }}</p>
                </div>
              }
            </section>

            @if (!current.convertedClientId) {
              <section class="card card--padded section-gap">
                <span class="eyebrow">Cierre</span>
                <h2>Convertir a cliente</h2>
                <p class="muted">
                  Primero revisamos coincidencias; tú decides si se crea o relaciona.
                </p>
                <button
                  class="btn btn--secondary btn--full"
                  type="button"
                  (click)="openConversion()"
                >
                  Revisar conversión
                </button>
              </section>
            } @else {
              <section class="card card--padded section-gap">
                <span class="eyebrow">Cliente convertido</span>
                <p>El historial comercial se conserva en este prospecto.</p>
                <a
                  class="btn btn--secondary btn--full"
                  [routerLink]="['/app/clients', current.convertedClientId]"
                >
                  Abrir cliente
                </a>
              </section>
              <section class="card card--padded section-gap">
                <span class="eyebrow">Siguiente etapa</span>
                <h2>Evento preliminar</h2>
                <p class="muted">Relaciona la oportunidad sin confirmar todavía la contratación.</p>
                <button
                  class="btn btn--quiet btn--full"
                  type="button"
                  (click)="showEvent.set(true)"
                >
                  Crear evento preliminar
                </button>
              </section>
            }

            <section class="card card--padded section-gap">
              <span class="eyebrow">Propuesta</span>
              <h2>Preparar oferta</h2>
              <p class="muted">Crea un borrador usando el catálogo comercial.</p>
              <a
                class="btn btn--primary btn--full"
                [routerLink]="['/app/proposals/new']"
                [queryParams]="{ prospectId: current.id }"
              >
                Crear propuesta
              </a>
            </section>
          </aside>
        </div>

        @if (showConversion()) {
          <div class="modal-layer" role="dialog" aria-modal="true">
            <section class="modal card card--padded">
              <div class="section-heading">
                <div>
                  <span class="eyebrow">Conversión controlada</span>
                  <h2>Elegir cliente</h2>
                </div>
                <button class="icon-button" type="button" (click)="showConversion.set(false)">
                  ×
                </button>
              </div>
              @if (matchesLoading()) {
                <div class="skeleton skeleton--row"></div>
              } @else {
                @if (matches().length) {
                  <p>Encontramos posibles coincidencias dentro de esta organización:</p>
                  <div class="choice-list">
                    @for (match of matches(); track match.clientId) {
                      <label class="choice-card">
                        <input
                          type="radio"
                          name="clientMatch"
                          [value]="match.clientId"
                          [(ngModel)]="selectedClientId"
                        />
                        <span>
                          <strong>{{ match.displayName }}</strong>
                          <small>{{ match.matchField }}: {{ match.matchValue }}</small>
                        </span>
                      </label>
                    }
                  </div>
                } @else {
                  <p>No encontramos clientes con el mismo correo o teléfono.</p>
                }
                <label class="choice-card">
                  <input type="radio" name="clientMatch" value="" [(ngModel)]="selectedClientId" />
                  <span
                    ><strong>Crear cliente nuevo</strong
                    ><small>No se crea una cuenta de acceso.</small></span
                  >
                </label>
                <div class="form-actions section-gap">
                  <button class="btn btn--quiet" type="button" (click)="showConversion.set(false)">
                    Cancelar
                  </button>
                  <button
                    class="btn btn--primary"
                    type="button"
                    [disabled]="converting()"
                    (click)="convert()"
                  >
                    Confirmar conversión
                  </button>
                </div>
              }
            </section>
          </div>
        }
        @if (showEvent()) {
          <div class="modal-layer" role="dialog" aria-modal="true">
            <form class="modal card card--padded form-stack" (ngSubmit)="createPreliminaryEvent()">
              <div class="section-heading">
                <div>
                  <span class="eyebrow">Sin confirmar</span>
                  <h2>Nuevo evento preliminar</h2>
                </div>
                <button class="icon-button" type="button" (click)="showEvent.set(false)">×</button>
              </div>
              <div class="form-grid">
                <label class="span-2"
                  >Nombre del evento<input name="eventName" [(ngModel)]="eventDraft.name" required
                /></label>
                <label
                  >Tipo<input name="eventType" [(ngModel)]="eventDraft.eventType" required
                /></label>
                <label
                  >Fecha estimada<input
                    name="eventStart"
                    type="datetime-local"
                    [(ngModel)]="eventStartLocal"
                    required
                /></label>
                <label
                  >Ciudad<input name="eventCity" [(ngModel)]="eventDraft.city" required
                /></label>
                <label
                  >Invitados estimados<input
                    name="eventGuests"
                    type="number"
                    min="1"
                    [(ngModel)]="eventDraft.estimatedGuestCount"
                /></label>
              </div>
              <p class="calculation-note">
                La confirmación ocurrirá en la etapa de contrato y anticipo del Sprint 1B.
              </p>
              <div class="form-actions">
                <button class="btn btn--quiet" type="button" (click)="showEvent.set(false)">
                  Cancelar
                </button>
                <button class="btn btn--primary" type="submit" [disabled]="savingEvent()">
                  {{ savingEvent() ? 'Creando…' : 'Crear preliminar' }}
                </button>
              </div>
            </form>
          </div>
        }
      }
    </div>
  `,
})
export class ProspectDetailPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly prospectId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly prospect = signal<ProspectResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly changingStatus = signal(false);
  protected readonly savingActivity = signal(false);
  protected readonly showConversion = signal(false);
  protected readonly matchesLoading = signal(false);
  protected readonly matches = signal<ClientMatchSuggestion[]>([]);
  protected readonly converting = signal(false);
  protected readonly showEvent = signal(false);
  protected readonly savingEvent = signal(false);
  protected nextStatus: ProspectStatus | '' = '';
  protected statusReason = '';
  protected selectedClientId = '';
  protected eventStartLocal = '';
  protected eventDraft = {
    name: '',
    eventType: '',
    city: '',
    estimatedGuestCount: null as number | null,
  };
  protected activity: CreateProspectActivityRequest = {
    activityType: 'FollowUp',
    subject: '',
    description: null,
    scheduledAt: null,
    completedAt: null,
    assignedUserId: null,
    visibility: 'Internal',
  };

  constructor() {
    this.load();
  }

  protected allowedStatuses(status: ProspectStatus): ProspectStatus[] {
    const transitions: Record<ProspectStatus, ProspectStatus[]> = {
      New: ['Contacted', 'Qualified', 'Lost', 'Archived'],
      Contacted: ['Qualified', 'Opportunity', 'Lost', 'Archived'],
      Qualified: ['Opportunity', 'Lost', 'Archived'],
      Opportunity: ['ProposalDraft', 'ProposalSent', 'Negotiation', 'Won', 'Lost', 'Archived'],
      ProposalDraft: ['ProposalSent', 'Negotiation', 'Won', 'Lost', 'Archived'],
      ProposalSent: ['Negotiation', 'Won', 'Lost', 'Archived'],
      Negotiation: ['Won', 'Lost', 'Archived'],
      Won: ['Archived'],
      Lost: ['Contacted', 'Archived'],
      Archived: [],
    };
    return transitions[status];
  }

  protected statusLabel(status: ProspectStatus): string {
    const labels: Record<ProspectStatus, string> = {
      New: 'Nuevo',
      Contacted: 'Contactado',
      Qualified: 'Calificado',
      Opportunity: 'Oportunidad',
      ProposalDraft: 'Propuesta en borrador',
      ProposalSent: 'Propuesta enviada',
      Negotiation: 'Negociación',
      Won: 'Ganado',
      Lost: 'Perdido',
      Archived: 'Archivado',
    };
    return labels[status];
  }

  protected activityLabel(type: string): string {
    const labels: Record<string, string> = {
      Note: 'Nota',
      Call: 'Llamada',
      WhatsApp: 'WhatsApp',
      Email: 'Correo',
      Meeting: 'Reunión',
      FollowUp: 'Seguimiento',
      StatusChange: 'Cambio de estado',
      ProposalSent: 'Propuesta enviada',
      ClientComment: 'Comentario del cliente',
    };
    return labels[type] ?? type;
  }

  protected changeStatus(): void {
    if (!this.nextStatus || (this.nextStatus === 'Lost' && !this.statusReason.trim())) {
      this.toast.error('Selecciona un estado y registra el motivo cuando corresponda.');
      return;
    }
    this.changingStatus.set(true);
    this.api
      .changeProspectStatus(
        this.organization.requireOrganizationId(),
        this.prospectId,
        this.nextStatus,
        this.statusReason.trim() || null,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.prospect.set(response);
          this.nextStatus = '';
          this.statusReason = '';
          this.changingStatus.set(false);
          this.toast.success('Estado actualizado.');
        },
        error: (error: unknown) => {
          this.changingStatus.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected addActivity(): void {
    if (!this.activity.subject.trim() || this.savingActivity()) {
      return;
    }
    this.savingActivity.set(true);
    const request: CreateProspectActivityRequest = {
      ...this.activity,
      subject: this.activity.subject.trim(),
      description: this.activity.description?.trim() || null,
      scheduledAt: this.activity.scheduledAt
        ? new Date(this.activity.scheduledAt).toISOString()
        : null,
    };
    this.api
      .addProspectActivity(this.organization.requireOrganizationId(), this.prospectId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.savingActivity.set(false);
          this.activity = { ...this.activity, subject: '', description: null, scheduledAt: null };
          this.toast.success('Actividad registrada.');
          this.load();
        },
        error: (error: unknown) => {
          this.savingActivity.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected completeActivity(activityId: string): void {
    this.api
      .completeProspectActivity(
        this.organization.requireOrganizationId(),
        this.prospectId,
        activityId,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.load(),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected openConversion(): void {
    this.showConversion.set(true);
    this.matchesLoading.set(true);
    this.api
      .getProspectMatches(this.organization.requireOrganizationId(), this.prospectId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (matches) => {
          this.matches.set(matches);
          this.matchesLoading.set(false);
        },
        error: (error: unknown) => {
          this.matchesLoading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected convert(): void {
    this.converting.set(true);
    this.api
      .convertProspect(this.organization.requireOrganizationId(), this.prospectId, {
        existingClientId: this.selectedClientId || null,
        newClientType: this.selectedClientId ? null : 'Person',
        confirmCreateDespiteMatches: !this.selectedClientId,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.converting.set(false);
          this.showConversion.set(false);
          this.toast.success('Prospecto convertido sin perder su historial.');
          this.load();
        },
        error: (error: unknown) => {
          this.converting.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected createPreliminaryEvent(): void {
    if (
      !this.eventDraft.name.trim() ||
      !this.eventDraft.eventType.trim() ||
      !this.eventDraft.city.trim() ||
      !this.eventStartLocal
    ) {
      this.toast.error('Completa los datos del evento preliminar.');
      return;
    }
    this.savingEvent.set(true);
    this.api
      .linkProspectPreliminaryEvent(this.organization.requireOrganizationId(), this.prospectId, {
        existingEventId: null,
        name: this.eventDraft.name.trim(),
        eventType: this.eventDraft.eventType.trim(),
        startDateTime: new Date(this.eventStartLocal).toISOString(),
        timeZone: 'America/Matamoros',
        city: this.eventDraft.city.trim(),
        countryCode: 'MX',
        estimatedGuestCount: this.eventDraft.estimatedGuestCount,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.savingEvent.set(false);
          this.showEvent.set(false);
          this.toast.success('Evento preliminar creado y relacionado.');
          void this.router.navigate(['/app/events', response.eventId]);
        },
        error: (error: unknown) => {
          this.savingEvent.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  private load(): void {
    this.loading.set(true);
    this.api
      .getProspect(this.organization.requireOrganizationId(), this.prospectId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.prospect.set(response);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }
}

import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  ContractingReadiness,
  DocumentResponse,
  PortalEventDetail,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-portal-event-detail-page',
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-page">
      <a class="back-link" routerLink="/portal/events">← Mis eventos</a>
      @if (event(); as currentEvent) {
        <header class="portal-event-hero">
          <div class="portal-event-hero__date">
            <span>{{ currentEvent.startDateTime | date: 'MMMM' }}</span>
            <strong>{{ currentEvent.startDateTime | date: 'dd' }}</strong>
            <small>{{ currentEvent.startDateTime | date: 'yyyy' }}</small>
          </div>
          <div>
            <span class="eyebrow">{{ currentEvent.eventType }}</span>
            <h1>{{ currentEvent.name }}</h1>
            <p>{{ currentEvent.city }}, {{ currentEvent.countryCode }}</p>
            <div class="button-row">
              <a
                class="btn btn--primary"
                [routerLink]="['/portal/events', eventId, 'guest-experience']"
              >
                Colaborar en invitados
              </a>
              <a
                class="btn btn--secondary"
                [routerLink]="['/portal/events', eventId, 'rsvp']"
              >
                Ver RSVP
              </a>
            </div>
          </div>
        </header>

        @if (readiness(); as state) {
          <section class="panel portal-contracting-summary">
            <div class="section-heading">
              <div>
                <span class="eyebrow">Contratación</span>
                <h2>Estado de tu evento</h2>
              </div>
              <span class="status-chip" [attr.data-status]="state.eventStatus">
                {{ state.eventStatus === 'Confirmed' ? 'Confirmado' : 'Preliminar' }}
              </span>
            </div>
            <div class="readiness-grid">
              <div [class.is-complete]="state.proposalAccepted">
                <span>{{ state.proposalAccepted ? '✓' : '1' }}</span>
                <strong>Propuesta</strong>
                <small>{{ state.proposalAccepted ? 'Aceptada' : 'Pendiente' }}</small>
              </div>
              <div [class.is-complete]="state.contractCompleted">
                <span>{{ state.contractCompleted ? '✓' : '2' }}</span>
                <strong>Contrato</strong>
                <small>{{ state.contractCompleted ? 'Completado' : 'Pendiente' }}</small>
              </div>
              <div [class.is-complete]="state.depositSatisfied">
                <span>{{ state.depositSatisfied ? '✓' : '3' }}</span>
                <strong>Anticipo</strong>
                <small>
                  {{ state.approvedDepositAmount }} de {{ state.requiredDepositAmount }}
                </small>
              </div>
            </div>
            <div class="button-row">
              <a class="btn btn--secondary" routerLink="/portal/contracts">Ver contratos</a>
              <a class="btn btn--secondary" routerLink="/portal/payments">Ver pagos</a>
            </div>
          </section>
        }

        <section class="portal-detail-grid">
          <article class="card card--padded portal-story">
            <span class="eyebrow">Información compartida</span>
            <h2>Sobre el evento</h2>
            <p>
              {{
                currentEvent.sharedDescription ?? 'Tu planner pronto compartirá más información.'
              }}
            </p>
            <dl class="detail-list">
              <div>
                <dt>Inicio</dt>
                <dd>{{ currentEvent.startDateTime | date: 'medium' }}</dd>
              </div>
              <div>
                <dt>Fin</dt>
                <dd>
                  {{
                    currentEvent.endDateTime
                      ? (currentEvent.endDateTime | date: 'medium')
                      : 'Por definir'
                  }}
                </dd>
              </div>
              <div>
                <dt>Invitados estimados</dt>
                <dd>{{ currentEvent.estimatedGuestCount ?? 'Por definir' }}</dd>
              </div>
              <div>
                <dt>Zona horaria</dt>
                <dd>{{ currentEvent.timeZone }}</dd>
              </div>
            </dl>
          </article>

          <article class="card card--padded">
            <span class="eyebrow">Protagonistas</span>
            <h2>Participantes</h2>
            @for (participant of currentEvent.participants; track participant.id) {
              <div class="contact-row">
                <span class="avatar avatar--soft">{{ participant.displayName.charAt(0) }}</span>
                <span>
                  <strong>{{ participant.displayName }}</strong>
                  <small>{{ participant.participantType }}</small>
                  @if (participant.sharedDescription) {
                    <p>{{ participant.sharedDescription }}</p>
                  }
                </span>
              </div>
            } @empty {
              <p class="muted">Aún no hay participantes compartidos.</p>
            }
          </article>
        </section>

        <section class="card card--padded section-gap">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Archivos</span>
              <h2>Documentos compartidos</h2>
            </div>
          </div>
          @for (document of currentEvent.documents; track document.id) {
            <div class="document-row">
              <span class="document-row__icon">
                {{ document.mimeType === 'application/pdf' ? 'PDF' : 'IMG' }}
              </span>
              <span>
                <strong>{{ document.fileName }}</strong>
                <small>{{ document.documentType }} · {{ formatBytes(document.sizeBytes) }}</small>
              </span>
              <button class="btn btn--secondary" type="button" (click)="download(document)">
                Descargar
              </button>
            </div>
          } @empty {
            <p class="muted">Tu planner aún no ha compartido documentos.</p>
          }
        </section>
      } @else {
        <div class="skeleton skeleton--hero"></div>
      }
    </div>
  `,
})
export class PortalEventDetailPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly eventId =
    this.route.snapshot.paramMap.get('id') ??
    (() => {
      throw new Error('La ruta requiere un evento.');
    })();
  protected readonly event = signal<PortalEventDetail | null>(null);
  protected readonly readiness = signal<ContractingReadiness | null>(null);

  constructor() {
    forkJoin({
      event: this.api.getPortalEvent(this.eventId),
      readiness: this.api.getPortalContractingReadiness(this.eventId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ event, readiness }) => {
          this.event.set(event);
          this.readiness.set(readiness);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected download(document: DocumentResponse): void {
    this.api
      .downloadPortalDocument(this.eventId, document.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const anchor = window.document.createElement('a');
          anchor.href = url;
          anchor.download = document.fileName;
          anchor.click();
          URL.revokeObjectURL(url);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected formatBytes(size: number): string {
    return size >= 1024 * 1024
      ? `${(size / (1024 * 1024)).toFixed(1)} MB`
      : `${Math.ceil(size / 1024)} KB`;
  }
}

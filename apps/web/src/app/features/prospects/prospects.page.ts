import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  ProspectDetailsRequest,
  ProspectListItem,
  ProspectStatus,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

interface PipelineColumn {
  status: ProspectStatus;
  label: string;
}

@Component({
  selector: 'app-prospects-page',
  imports: [FormsModule, RouterLink, CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page page--wide">
      <header class="page-header">
        <div>
          <span class="eyebrow">CRM comercial</span>
          <h1>Pipeline</h1>
          <p>Da seguimiento a cada oportunidad desde el primer contacto hasta el cierre.</p>
        </div>
        @if (organization.hasPermission('prospects.create')) {
          <button class="btn btn--primary" type="button" (click)="showCreate.set(true)">
            ＋ Nuevo prospecto
          </button>
        }
      </header>

      <section class="card card--padded pipeline-toolbar">
        <label>
          Buscar
          <input
            type="search"
            [(ngModel)]="search"
            placeholder="Nombre, correo o teléfono"
            (keyup.enter)="load()"
          />
        </label>
        <label>
          Tipo de evento
          <input [(ngModel)]="eventType" placeholder="Boda, XV años…" />
        </label>
        <label>
          Desde
          <input type="date" [(ngModel)]="dateFrom" />
        </label>
        <label>
          Hasta
          <input type="date" [(ngModel)]="dateTo" />
        </label>
        <button class="btn btn--quiet" type="button" (click)="load()">Aplicar filtros</button>
      </section>

      @if (loading()) {
        <div class="card card--padded section-gap">
          <div class="skeleton skeleton--row"></div>
          <div class="skeleton skeleton--row"></div>
        </div>
      } @else {
        <div class="pipeline-board" aria-label="Pipeline de prospectos">
          @for (column of columns; track column.status) {
            <section class="pipeline-column">
              <header>
                <span class="pipeline-dot" [attr.data-status]="column.status"></span>
                <strong>{{ column.label }}</strong>
                <span>{{ prospectsFor(column.status).length }}</span>
              </header>
              <div class="pipeline-column__body">
                @for (prospect of prospectsFor(column.status); track prospect.id) {
                  <a class="prospect-card" [routerLink]="['/app/prospects', prospect.id]">
                    <span class="prospect-card__type">
                      {{ prospect.eventTypeInterest ?? 'Evento por definir' }}
                    </span>
                    <strong>{{ prospect.displayName }}</strong>
                    <small>
                      {{
                        prospect.estimatedEventDate
                          ? (prospect.estimatedEventDate | date: 'dd MMM yyyy')
                          : 'Fecha por definir'
                      }}
                    </small>
                    @if (prospect.estimatedBudget !== null) {
                      <span class="prospect-card__amount">
                        {{ prospect.estimatedBudget | currency: prospect.currencyCode }}
                      </span>
                    }
                  </a>
                } @empty {
                  <div class="pipeline-empty">Sin oportunidades</div>
                }
              </div>
            </section>
          }
        </div>
      }

      @if (showCreate()) {
        <div
          class="modal-layer"
          role="dialog"
          aria-modal="true"
          aria-labelledby="new-prospect-title"
        >
          <form class="modal card card--padded form-stack" (ngSubmit)="create()">
            <div class="section-heading">
              <div>
                <span class="eyebrow">Nueva oportunidad</span>
                <h2 id="new-prospect-title">Registrar prospecto</h2>
              </div>
              <button class="icon-button" type="button" (click)="showCreate.set(false)">×</button>
            </div>
            <div class="form-grid">
              <label class="span-2">
                Nombre para mostrar
                <input
                  name="displayName"
                  [(ngModel)]="draft.displayName"
                  required
                  maxlength="180"
                />
              </label>
              <label>
                Nombre
                <input name="firstName" [(ngModel)]="draft.firstName" />
              </label>
              <label>
                Apellidos
                <input name="lastName" [(ngModel)]="draft.lastName" />
              </label>
              <label>
                Correo
                <input name="email" type="email" [(ngModel)]="draft.email" />
              </label>
              <label>
                Teléfono
                <input name="phone" type="tel" [(ngModel)]="draft.phone" />
              </label>
              <label>
                Tipo de evento
                <input name="eventType" [(ngModel)]="draft.eventTypeInterest" />
              </label>
              <label>
                Fecha estimada
                <input name="estimatedDate" type="date" [(ngModel)]="draft.estimatedEventDate" />
              </label>
              <label>
                Presupuesto estimado
                <input name="budget" type="number" min="0" [(ngModel)]="draft.estimatedBudget" />
              </label>
              <label>
                Ciudad
                <input name="city" [(ngModel)]="draft.city" />
              </label>
              <label class="span-2">
                Notas internas
                <textarea name="notes" [(ngModel)]="draft.notes"></textarea>
              </label>
            </div>
            <div class="form-actions">
              <button class="btn btn--quiet" type="button" (click)="showCreate.set(false)">
                Cancelar
              </button>
              <button class="btn btn--primary" type="submit" [disabled]="saving()">
                {{ saving() ? 'Guardando…' : 'Crear prospecto' }}
              </button>
            </div>
          </form>
        </div>
      }
    </div>
  `,
})
export class ProspectsPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly prospects = signal<ProspectListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly showCreate = signal(false);
  protected search = '';
  protected eventType = '';
  protected dateFrom = '';
  protected dateTo = '';
  protected readonly columns: PipelineColumn[] = [
    { status: 'New', label: 'Nuevos' },
    { status: 'Contacted', label: 'Contactados' },
    { status: 'Qualified', label: 'Calificados' },
    { status: 'Opportunity', label: 'Oportunidad' },
    { status: 'ProposalDraft', label: 'Propuesta' },
    { status: 'ProposalSent', label: 'Enviada' },
    { status: 'Negotiation', label: 'Negociación' },
    { status: 'Won', label: 'Ganados' },
    { status: 'Lost', label: 'Perdidos' },
  ];
  protected draft: ProspectDetailsRequest = this.emptyDraft();

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.api
      .getProspects(this.organization.requireOrganizationId(), {
        search: this.search || undefined,
        eventType: this.eventType || undefined,
        dateFrom: this.dateFrom || undefined,
        dateTo: this.dateTo || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.prospects.set(response.items);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected prospectsFor(status: ProspectStatus): ProspectListItem[] {
    return this.prospects().filter((prospect) => prospect.status === status);
  }

  protected create(): void {
    if (!this.draft.displayName.trim() || this.saving()) {
      return;
    }
    this.saving.set(true);
    this.api
      .createProspect(this.organization.requireOrganizationId(), this.normalizeDraft(this.draft))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (prospect) => {
          this.saving.set(false);
          this.toast.success('Prospecto registrado.');
          void this.router.navigate(['/app/prospects', prospect.id]);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  private emptyDraft(): ProspectDetailsRequest {
    return {
      displayName: '',
      firstName: null,
      lastName: null,
      companyName: null,
      email: null,
      phone: null,
      source: null,
      eventTypeInterest: null,
      estimatedEventDate: null,
      estimatedGuestCount: null,
      estimatedBudget: null,
      currencyCode: 'MXN',
      city: null,
      notes: null,
      assignedUserId: null,
    };
  }

  private normalizeDraft(request: ProspectDetailsRequest): ProspectDetailsRequest {
    const nullable = (value: string | null): string | null => (value?.trim() ? value.trim() : null);
    return {
      ...request,
      displayName: request.displayName.trim(),
      firstName: nullable(request.firstName),
      lastName: nullable(request.lastName),
      email: nullable(request.email),
      phone: nullable(request.phone),
      eventTypeInterest: nullable(request.eventTypeInterest),
      city: nullable(request.city),
      notes: nullable(request.notes),
    };
  }
}

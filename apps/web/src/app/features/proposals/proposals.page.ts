import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ProposalListItem, ProposalStatus } from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-proposals-page',
  imports: [FormsModule, RouterLink, CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Ventas</span>
          <h1>Propuestas</h1>
          <p>Borradores, versiones enviadas y decisiones del cliente en un solo lugar.</p>
        </div>
        @if (organization.hasPermission('proposals.create')) {
          <a class="btn btn--primary" routerLink="/app/proposals/new">＋ Nueva propuesta</a>
        }
      </header>
      <section class="card">
        <div class="toolbar">
          <label class="search-field">
            <span aria-hidden="true">⌕</span>
            <input
              type="search"
              [(ngModel)]="search"
              (keyup.enter)="load()"
              placeholder="Buscar por folio o destinatario"
            />
          </label>
          <select class="toolbar-select" [(ngModel)]="status" (change)="load()" aria-label="Estado">
            <option value="">Todos los estados</option>
            @for (option of statuses; track option.value) {
              <option [value]="option.value">{{ option.label }}</option>
            }
          </select>
          <button class="btn btn--quiet" type="button" (click)="load()">Buscar</button>
          <span class="toolbar__count">{{ proposals().length }} propuestas</span>
        </div>
        @if (loading()) {
          <div class="list-skeleton">
            <div class="skeleton skeleton--row"></div>
            <div class="skeleton skeleton--row"></div>
          </div>
        } @else {
          <div class="responsive-table">
            <table>
              <thead>
                <tr>
                  <th>Propuesta</th>
                  <th>Destinatario</th>
                  <th>Versión</th>
                  <th>Total</th>
                  <th>Vigencia</th>
                  <th>Estado</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                @for (proposal of proposals(); track proposal.id) {
                  <tr>
                    <td>
                      <strong>{{ proposal.proposalNumber }}</strong
                      ><small class="table-subtitle"
                        >Actualizada {{ proposal.updatedAt | date: 'dd MMM' }}</small
                      >
                    </td>
                    <td>{{ proposal.targetDisplayName }}</td>
                    <td>v{{ proposal.currentVersionNumber }}</td>
                    <td>
                      <strong>{{
                        proposal.grandTotal ?? 0 | currency: proposal.currencyCode
                      }}</strong>
                    </td>
                    <td>{{ proposal.validUntil | date: 'dd MMM yyyy' }}</td>
                    <td>
                      <span class="status-chip" [attr.data-status]="proposal.status">{{
                        statusLabel(proposal.status)
                      }}</span>
                    </td>
                    <td>
                      <a
                        class="icon-button"
                        [routerLink]="['/app/proposals', proposal.id]"
                        aria-label="Abrir propuesta"
                        >→</a
                      >
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="7">
                      <div class="empty-state">
                        <span class="empty-state__icon">◇</span>
                        <h2>No hay propuestas todavía</h2>
                        <p>Crea la primera desde un prospecto o directamente aquí.</p>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>
    </div>
  `,
})
export class ProposalsPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly proposals = signal<ProposalListItem[]>([]);
  protected readonly loading = signal(true);
  protected search = '';
  protected status: ProposalStatus | '' = '';
  protected readonly statuses: { value: ProposalStatus; label: string }[] = [
    { value: 'Draft', label: 'Borrador' },
    { value: 'Ready', label: 'Lista' },
    { value: 'Sent', label: 'Enviada' },
    { value: 'Viewed', label: 'Vista' },
    { value: 'ChangesRequested', label: 'Cambios solicitados' },
    { value: 'Negotiation', label: 'Negociación' },
    { value: 'Accepted', label: 'Aceptada' },
    { value: 'Rejected', label: 'Rechazada' },
    { value: 'Expired', label: 'Vencida' },
    { value: 'Cancelled', label: 'Cancelada' },
  ];

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.api
      .getProposals(
        this.organization.requireOrganizationId(),
        this.search,
        this.status || undefined,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.proposals.set(response.items);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected statusLabel(status: ProposalStatus): string {
    return this.statuses.find((item) => item.value === status)?.label ?? status;
  }
}

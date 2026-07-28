import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ProposalListItem } from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-portal-proposals-page',
  imports: [RouterLink, CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Portal del cliente</span>
          <h1>Mis propuestas</h1>
          <p>Consulta las versiones compartidas para tus eventos.</p>
        </div>
      </header>
      <section class="catalog-grid">
        @for (proposal of proposals(); track proposal.id) {
          <a
            class="catalog-card card card--padded"
            [routerLink]="['/portal/proposals', proposal.id]"
          >
            <div class="catalog-card__topline">
              <span>{{ proposal.proposalNumber }}</span
              ><span class="status-chip" [attr.data-status]="proposal.status">{{
                proposal.status
              }}</span>
            </div>
            <h2>{{ proposal.targetDisplayName }}</h2>
            <p>
              Versión {{ proposal.currentVersionNumber }} · válida hasta
              {{ proposal.validUntil | date: 'dd MMM yyyy' }}
            </p>
            <strong class="catalog-price">{{
              proposal.grandTotal ?? 0 | currency: proposal.currencyCode
            }}</strong>
          </a>
        } @empty {
          @if (!loading()) {
            <div class="card card--padded empty-state">
              <h2>No hay propuestas compartidas</h2>
              <p>Cuando tu planner comparta una, aparecerá aquí.</p>
            </div>
          }
        }
      </section>
    </div>
  `,
})
export class PortalProposalsPage {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly proposals = signal<ProposalListItem[]>([]);
  protected readonly loading = signal(true);

  constructor() {
    this.api
      .getPortalProposals()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.proposals.set(response);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }
}

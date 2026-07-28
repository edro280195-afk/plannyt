import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ProposalPublicResponse } from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-portal-proposal-detail-page',
  imports: [RouterLink, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-page page--narrow">
      <a class="back-link" routerLink="/portal/proposals">← Mis propuestas</a>
      @if (proposal(); as current) {
        <section class="card card--padded detail-hero">
          <div>
            <span class="eyebrow"
              >{{ current.proposalNumber }} · versión {{ current.versionNumber }}</span
            >
            <h1>{{ current.recipientName }}</h1>
            <p>{{ current.sharedIntroduction }}</p>
          </div>
          <div>
            <span class="status-chip" [attr.data-status]="current.status">{{
              current.status
            }}</span>
            <button
              class="btn btn--quiet section-gap"
              type="button"
              (click)="downloadPdf(current.proposalId)"
            >
              Descargar PDF
            </button>
          </div>
        </section>
        <section class="card section-gap">
          @for (line of current.lines; track line.id) {
            <article class="public-line">
              <div>
                <strong>{{ line.description }}</strong
                ><small
                  >{{ line.quantity }} × {{ line.unitPrice | currency: current.currencyCode }}
                  @if (line.isOptional) {
                    · Opcional
                  }
                </small>
              </div>
              <strong>{{ line.lineTotal | currency: current.currencyCode }}</strong>
            </article>
          }
          <footer class="public-totals">
            <div>
              <span>Subtotal</span
              ><strong>{{ current.totals.subtotal | currency: current.currencyCode }}</strong>
            </div>
            <div>
              <span>Impuestos</span
              ><strong>{{ current.totals.taxTotal | currency: current.currencyCode }}</strong>
            </div>
            <div class="public-totals__grand">
              <span>Total</span
              ><strong>{{ current.totals.grandTotal | currency: current.currencyCode }}</strong>
            </div>
          </footer>
        </section>
      }
    </div>
  `,
})
export class PortalProposalDetailPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly proposal = signal<ProposalPublicResponse | null>(null);

  constructor() {
    const proposalId = this.route.snapshot.paramMap.get('id') ?? '';
    this.api
      .getPortalProposal(proposalId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => this.proposal.set(response),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected downloadPdf(proposalId: string): void {
    this.api
      .downloadPortalProposalPdf(proposalId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `propuesta-${proposalId}.pdf`;
          link.click();
          URL.revokeObjectURL(url);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }
}

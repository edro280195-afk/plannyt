import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ProposalPublicResponse } from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-public-proposal-page',
  imports: [FormsModule, CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="public-proposal">
      <header class="public-proposal__header">
        <a class="brand brand--compact" href="/">
          <span class="brand__mark">P</span>
          <span><strong>Plannyt</strong><small>Propuesta privada</small></span>
        </a>
        <span class="secure-label">Enlace privado · Solo para ti</span>
      </header>

      @if (loading()) {
        <main class="public-proposal__main">
          <div class="card card--padded"><div class="skeleton skeleton--row"></div></div>
        </main>
      } @else if (proposal(); as current) {
        <main class="public-proposal__main">
          <section class="proposal-cover">
            <div>
              <span class="eyebrow"
                >Propuesta {{ current.proposalNumber }} · versión {{ current.versionNumber }}</span
              >
              <h1>{{ current.recipientName }}, hagamos realidad tu evento.</h1>
              <p>
                {{
                  current.sharedIntroduction ??
                    'Hemos preparado una propuesta clara y flexible para ti.'
                }}
              </p>
              @if (current.eventSummary) {
                <span class="event-summary">{{ current.eventSummary }}</span>
              }
            </div>
            <div class="proposal-cover__meta">
              <span class="status-chip" [attr.data-status]="current.status">{{
                statusLabel(current.status)
              }}</span>
              <small>Válida hasta</small>
              <strong>{{ current.validUntil | date: 'dd MMMM yyyy' }}</strong>
              <button class="btn btn--quiet btn--full" type="button" (click)="downloadPdf()">
                Descargar PDF
              </button>
            </div>
          </section>

          <section class="public-lines card">
            <header class="public-section-heading">
              <div>
                <span class="eyebrow">Inversión</span>
                <h2>Conceptos incluidos</h2>
              </div>
            </header>
            @for (line of requiredLines(); track line.id) {
              <article class="public-line">
                <div>
                  <strong>{{ line.description }}</strong>
                  <small
                    >{{ line.quantity }} ×
                    {{ line.unitPrice | currency: current.currencyCode }}</small
                  >
                </div>
                <strong>{{ line.lineTotal | currency: current.currencyCode }}</strong>
              </article>
            }
            @if (optionalLines().length) {
              <div class="optional-heading">
                <span>Opciones adicionales</span><small>No incluidas en el total</small>
              </div>
              @for (line of optionalLines(); track line.id) {
                <article class="public-line public-line--optional">
                  <div>
                    <strong>{{ line.description }}</strong
                    ><small
                      >{{ line.quantity }} ×
                      {{ line.unitPrice | currency: current.currencyCode }}</small
                    >
                  </div>
                  <strong>＋ {{ line.lineTotal | currency: current.currencyCode }}</strong>
                </article>
              }
            }
            <footer class="public-totals">
              <div>
                <span>Subtotal</span
                ><strong>{{ current.totals.subtotal | currency: current.currencyCode }}</strong>
              </div>
              @if (current.totals.discountTotal > 0) {
                <div class="discount-line">
                  <span>Descuentos</span
                  ><strong
                    >− {{ current.totals.discountTotal | currency: current.currencyCode }}</strong
                  >
                </div>
              }
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

          @if (current.sharedTerms) {
            <section class="card card--padded public-terms">
              <span class="eyebrow">Términos</span>
              <h2>Condiciones de la propuesta</h2>
              <p>{{ current.sharedTerms }}</p>
            </section>
          }

          <div class="public-bottom-grid">
            <section class="card card--padded">
              <span class="eyebrow">Conversación</span>
              <h2>Comentarios</h2>
              <div class="comment-list">
                @for (comment of current.comments; track comment.id) {
                  <article class="comment">
                    <div>
                      <strong>{{ comment.authorDisplayName }}</strong
                      ><small>{{ comment.createdAt | date: 'dd MMM, HH:mm' }}</small>
                    </div>
                    <p>{{ comment.content }}</p>
                  </article>
                } @empty {
                  <p class="muted">No hay comentarios todavía.</p>
                }
              </div>
              <form class="form-stack section-gap" (ngSubmit)="comment()">
                <label
                  >Tu nombre<input name="commentAuthor" [(ngModel)]="authorName" required
                /></label>
                <label
                  >Comentario<textarea
                    name="commentContent"
                    [(ngModel)]="commentContent"
                    required
                  ></textarea>
                </label>
                <button class="btn btn--secondary" type="submit" [disabled]="commenting()">
                  Enviar comentario
                </button>
              </form>
            </section>

            <section class="card card--padded decision-card">
              <span class="eyebrow">Tu decisión</span>
              @if (current.status === 'Accepted') {
                <div class="decision-result decision-result--success">
                  <span>✓</span>
                  <h2>Propuesta aceptada</h2>
                  <p>
                    El equipo continuará con la etapa de contratación. La aceptación todavía no
                    confirma el evento.
                  </p>
                </div>
              } @else if (current.status === 'Rejected') {
                <div class="decision-result">
                  <h2>Propuesta rechazada</h2>
                  <p>Tu decisión quedó registrada.</p>
                </div>
              } @else if (canDecide(current)) {
                <h2>¿Cómo quieres continuar?</h2>
                <p>Tu decisión se aplicará exactamente a la versión {{ current.versionNumber }}.</p>
                <label>Tu nombre<input [(ngModel)]="authorName" /></label>
                <label>Mensaje opcional<textarea [(ngModel)]="decisionReason"></textarea></label>
                <button
                  class="btn btn--primary btn--full"
                  type="button"
                  [disabled]="deciding()"
                  (click)="decide('accept')"
                >
                  Aceptar propuesta
                </button>
                <button
                  class="btn btn--secondary btn--full"
                  type="button"
                  [disabled]="deciding()"
                  (click)="decide('request-changes')"
                >
                  Solicitar cambios
                </button>
                <button
                  class="btn btn--quiet btn--full"
                  type="button"
                  [disabled]="deciding()"
                  (click)="decide('reject')"
                >
                  Rechazar
                </button>
                <small class="decision-note"
                  >Aceptar la propuesta no equivale a firmar un contrato ni realizar un pago.</small
                >
              } @else {
                <div class="decision-result">
                  <h2>Decisión no disponible</h2>
                  <p>Esta versión ya no admite cambios o decisiones.</p>
                </div>
              }
            </section>
          </div>
        </main>
        <footer class="public-proposal__footer">
          Propuesta preparada por {{ current.organizationName }} con Plannyt.
        </footer>
      }
    </div>
  `,
})
export class PublicProposalPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly token = this.route.snapshot.paramMap.get('token') ?? '';
  protected readonly proposal = signal<ProposalPublicResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly commenting = signal(false);
  protected readonly deciding = signal(false);
  protected authorName = '';
  protected commentContent = '';
  protected decisionReason = '';

  constructor() {
    this.load();
  }

  protected requiredLines() {
    return this.proposal()?.lines.filter((line) => !line.isOptional) ?? [];
  }

  protected optionalLines() {
    return this.proposal()?.lines.filter((line) => line.isOptional) ?? [];
  }

  protected statusLabel(status: string): string {
    const labels: Record<string, string> = {
      Sent: 'Enviada',
      Viewed: 'Revisada',
      ChangesRequested: 'Cambios solicitados',
      Negotiation: 'En negociación',
      Accepted: 'Aceptada',
      Rejected: 'Rechazada',
      Expired: 'Vencida',
      Cancelled: 'Cancelada',
    };
    return labels[status] ?? status;
  }

  protected canDecide(proposal: ProposalPublicResponse): boolean {
    return ['Sent', 'Viewed', 'ChangesRequested', 'Negotiation'].includes(proposal.status);
  }

  protected comment(): void {
    if (!this.authorName.trim() || !this.commentContent.trim()) {
      return;
    }
    this.commenting.set(true);
    this.api
      .addPublicProposalComment(this.token, {
        authorDisplayName: this.authorName.trim(),
        content: this.commentContent.trim(),
        proposalLineId: null,
        parentCommentId: null,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.commenting.set(false);
          this.commentContent = '';
          this.toast.success('Comentario enviado.');
          this.load();
        },
        error: (error: unknown) => {
          this.commenting.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected decide(action: 'request-changes' | 'accept' | 'reject'): void {
    this.deciding.set(true);
    this.api
      .decidePublicProposal(
        this.token,
        action,
        this.authorName.trim() || null,
        this.decisionReason.trim() || null,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.proposal.set(response);
          this.deciding.set(false);
          this.toast.success(
            action === 'accept'
              ? 'Propuesta aceptada.'
              : action === 'reject'
                ? 'Decisión registrada.'
                : 'Solicitud de cambios enviada.',
          );
        },
        error: (error: unknown) => {
          this.deciding.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected downloadPdf(): void {
    this.api
      .downloadPublicProposalPdf(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `propuesta-${this.proposal()?.proposalNumber ?? 'plannyt'}.pdf`;
          link.click();
          URL.revokeObjectURL(url);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private load(): void {
    this.api
      .getPublicProposal(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.proposal.set(response);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }
}

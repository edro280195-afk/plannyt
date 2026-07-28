import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  CatalogPackage,
  ClientListItem,
  Coupon,
  DiscountType,
  ProposalDraftLineRequest,
  ProposalDraftRequest,
  ProposalResponse,
  ProspectListItem,
  ServiceCatalogItem,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-proposal-builder-page',
  imports: [FormsModule, RouterLink, CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page proposal-workspace">
      <a class="back-link" routerLink="/app/proposals">← Volver a propuestas</a>
      <header class="page-header">
        <div>
          <span class="eyebrow">{{ proposal()?.proposalNumber ?? 'Nueva propuesta' }}</span>
          <h1>Constructor de propuesta</h1>
          <p>
            El cálculo mostrado es una vista previa; el backend recalcula al guardar y publicar.
          </p>
        </div>
        @if (proposal(); as current) {
          <span class="status-chip" [attr.data-status]="current.status">{{ current.status }}</span>
        }
      </header>

      @if (loading()) {
        <section class="card card--padded"><div class="skeleton skeleton--row"></div></section>
      } @else {
        <div class="proposal-layout">
          <main class="proposal-builder">
            <section class="card card--padded">
              <div class="section-heading"><h2>Destinatario y vigencia</h2></div>
              <div class="form-grid">
                <label>
                  Prospecto
                  <select [(ngModel)]="draft.prospectId" (change)="draft.clientId = null">
                    <option [ngValue]="null">Selecciona</option>
                    @for (item of prospects(); track item.id) {
                      <option [ngValue]="item.id">{{ item.displayName }}</option>
                    }
                  </select>
                </label>
                <label>
                  Cliente
                  <select [(ngModel)]="draft.clientId" (change)="draft.prospectId = null">
                    <option [ngValue]="null">Selecciona</option>
                    @for (item of clients(); track item.id) {
                      <option [ngValue]="item.id">{{ item.displayName }}</option>
                    }
                  </select>
                </label>
                <label>
                  Vigencia
                  <input type="datetime-local" [(ngModel)]="validUntilLocal" />
                </label>
                <label>
                  Moneda
                  <select [(ngModel)]="draft.currencyCode">
                    <option value="MXN">MXN</option>
                    <option value="USD">USD</option>
                  </select>
                </label>
                <label class="span-2">
                  Introducción compartida
                  <textarea
                    [(ngModel)]="draft.sharedIntroduction"
                    placeholder="Un mensaje breve para el cliente"
                  ></textarea>
                </label>
              </div>
            </section>

            <section class="card card--padded section-gap">
              <div class="section-heading">
                <div>
                  <span class="eyebrow">Conceptos</span>
                  <h2>Servicios y opciones</h2>
                </div>
                <div class="button-group">
                  <select #catalogChoice aria-label="Elegir servicio">
                    <option value="">Agregar servicio…</option>
                    @for (service of services(); track service.id) {
                      <option [value]="service.id">{{ service.name }}</option>
                    }
                  </select>
                  <button
                    class="btn btn--secondary"
                    type="button"
                    (click)="addService(catalogChoice.value); catalogChoice.value = ''"
                  >
                    Agregar
                  </button>
                </div>
              </div>
              <div class="button-row">
                @for (item of packages(); track item.id) {
                  <button class="tag-button" type="button" (click)="addPackage(item)">
                    ＋ {{ item.name }}
                  </button>
                }
                <button class="tag-button" type="button" (click)="addCustomLine()">
                  ＋ Concepto personalizado
                </button>
              </div>

              <div class="proposal-lines section-gap">
                @for (line of draft.lines; track line.sortOrder; let index = $index) {
                  <article class="proposal-line" [class.proposal-line--optional]="line.isOptional">
                    <div class="proposal-line__order">
                      <button
                        class="icon-button"
                        type="button"
                        [disabled]="index === 0"
                        (click)="moveLine(index, -1)"
                        aria-label="Subir concepto"
                      >
                        ↑
                      </button>
                      <button
                        class="icon-button"
                        type="button"
                        [disabled]="index === draft.lines.length - 1"
                        (click)="moveLine(index, 1)"
                        aria-label="Bajar concepto"
                      >
                        ↓
                      </button>
                    </div>
                    <div class="proposal-line__fields">
                      <label class="span-2"
                        >Descripción<input [(ngModel)]="line.description"
                      /></label>
                      <label
                        >Cantidad<input
                          type="number"
                          min="0.01"
                          step="0.01"
                          [(ngModel)]="line.quantity"
                      /></label>
                      <label
                        >Precio unitario<input
                          type="number"
                          min="0"
                          step="0.01"
                          [(ngModel)]="line.unitPrice"
                      /></label>
                      <label
                        >Descuento<select [(ngModel)]="line.discountType">
                          <option value="None">Sin descuento</option>
                          <option value="Percentage">Porcentaje</option>
                          <option value="FixedAmount">Monto fijo</option>
                        </select></label
                      >
                      <label
                        >Valor<input
                          type="number"
                          min="0"
                          step="0.01"
                          [(ngModel)]="line.discountValue"
                          [disabled]="line.discountType === 'None'"
                      /></label>
                      <label
                        >Impuesto %<input
                          type="number"
                          min="0"
                          max="100"
                          step="0.01"
                          [(ngModel)]="line.taxRate"
                      /></label>
                      <label class="checkbox-row"
                        ><input type="checkbox" [(ngModel)]="line.isOptional" /> Opción
                        adicional</label
                      >
                    </div>
                    <div class="proposal-line__total">
                      <strong>{{ lineTotal(line) | currency: draft.currencyCode }}</strong>
                      <button
                        class="btn btn--danger-quiet btn--small"
                        type="button"
                        (click)="removeLine(index)"
                      >
                        Quitar
                      </button>
                    </div>
                  </article>
                } @empty {
                  <div class="empty-state empty-state--compact">
                    <h3>Agrega al menos un concepto</h3>
                    <p>Elige un servicio, paquete o línea personalizada.</p>
                  </div>
                }
              </div>
            </section>

            <section class="card card--padded section-gap">
              <div class="section-heading"><h2>Condiciones</h2></div>
              <div class="form-grid">
                <label>
                  Descuento general
                  <select [(ngModel)]="draft.generalDiscountType">
                    <option value="None">Sin descuento</option>
                    <option value="Percentage">Porcentaje</option>
                    <option value="FixedAmount">Monto fijo</option>
                  </select>
                </label>
                <label>
                  Valor
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    [(ngModel)]="draft.generalDiscountValue"
                    [disabled]="draft.generalDiscountType === 'None'"
                  />
                </label>
                <label>
                  Cupón
                  <select [(ngModel)]="draft.couponId">
                    <option [ngValue]="null">Sin cupón</option>
                    @for (coupon of coupons(); track coupon.id) {
                      <option [ngValue]="coupon.id">{{ coupon.code }}</option>
                    }
                  </select>
                </label>
                <label class="span-2">
                  Términos compartidos
                  <textarea [(ngModel)]="draft.sharedTerms"></textarea>
                </label>
                <label class="span-2">
                  Notas internas
                  <textarea
                    [(ngModel)]="draft.internalNotes"
                    placeholder="Nunca se muestran al destinatario"
                  ></textarea>
                </label>
              </div>
            </section>

            @if (proposal(); as current) {
              <section class="card card--padded section-gap">
                <div class="section-heading">
                  <div>
                    <span class="eyebrow">Trazabilidad</span>
                    <h2>Versiones y comentarios</h2>
                  </div>
                </div>
                <div class="version-list">
                  @for (version of current.versions; track version.id) {
                    <div class="version-row">
                      <span class="version-badge">v{{ version.versionNumber }}</span>
                      <span>Publicada {{ version.publishedAt | date: 'dd MMM yyyy, HH:mm' }}</span>
                      <strong>{{ version.grandTotal | currency: version.currencyCode }}</strong>
                      <button
                        class="btn btn--quiet btn--small"
                        type="button"
                        (click)="downloadVersionPdf(version.id)"
                      >
                        PDF
                      </button>
                    </div>
                  } @empty {
                    <p class="muted">Publica el borrador para congelar la primera versión.</p>
                  }
                </div>
                @for (comment of current.comments; track comment.id) {
                  <article class="comment">
                    <div>
                      <strong>{{ comment.authorDisplayName }}</strong
                      ><small>{{ comment.createdAt | date: 'dd MMM, HH:mm' }}</small>
                    </div>
                    <p>{{ comment.content }}</p>
                    <span class="visibility-label">{{
                      comment.visibility === 'Internal' ? 'Interno' : 'Compartido'
                    }}</span>
                  </article>
                }
              </section>
            }
          </main>

          <aside class="proposal-summary">
            <section class="card card--padded proposal-summary__sticky">
              <span class="eyebrow">Vista previa</span>
              <h2>Resumen</h2>
              <dl class="totals-list">
                <div>
                  <dt>Subtotal</dt>
                  <dd>{{ previewSubtotal() | currency: draft.currencyCode }}</dd>
                </div>
                <div>
                  <dt>Descuentos</dt>
                  <dd>− {{ previewDiscount() | currency: draft.currencyCode }}</dd>
                </div>
                <div>
                  <dt>Impuestos</dt>
                  <dd>{{ previewTax() | currency: draft.currencyCode }}</dd>
                </div>
                <div class="totals-list__grand">
                  <dt>Total estimado</dt>
                  <dd>{{ previewTotal() | currency: draft.currencyCode }}</dd>
                </div>
              </dl>
              <p class="calculation-note">
                Los opcionales no se suman al total. El valor definitivo se calcula en el servidor.
              </p>
              <button
                class="btn btn--primary btn--full"
                type="button"
                [disabled]="saving()"
                (click)="save()"
              >
                {{ saving() ? 'Guardando…' : proposal() ? 'Guardar borrador' : 'Crear borrador' }}
              </button>
              @if (proposal(); as current) {
                @if (
                  current.status === 'Accepted' && organization.hasPermission('contracts.create')
                ) {
                  <a
                    class="btn btn--primary btn--full"
                    [routerLink]="['/app/contracts']"
                    [queryParams]="{ proposalId: current.id }"
                  >
                    Generar contrato
                  </a>
                }
                <button
                  class="btn btn--secondary btn--full"
                  type="button"
                  [disabled]="publishing()"
                  (click)="publish()"
                >
                  {{ current.currentVersionNumber ? 'Publicar nueva versión' : 'Publicar versión' }}
                </button>
                @if (canSend(current)) {
                  <button
                    class="btn btn--quiet btn--full"
                    type="button"
                    [disabled]="sending()"
                    (click)="send()"
                  >
                    Generar enlace privado
                  </button>
                }
              }
              @if (shareUrl()) {
                <div class="share-box">
                  <strong>Enlace listo</strong>
                  <input readonly [value]="shareUrl()" />
                  <button class="btn btn--secondary btn--full" type="button" (click)="copyLink()">
                    Copiar enlace
                  </button>
                </div>
              }
            </section>
          </aside>
        </div>
      }
    </div>
  `,
})
export class ProposalBuilderPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly proposalId = this.route.snapshot.paramMap.get('id');

  protected readonly proposal = signal<ProposalResponse | null>(null);
  protected readonly prospects = signal<ProspectListItem[]>([]);
  protected readonly clients = signal<ClientListItem[]>([]);
  protected readonly services = signal<ServiceCatalogItem[]>([]);
  protected readonly packages = signal<CatalogPackage[]>([]);
  protected readonly coupons = signal<Coupon[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly publishing = signal(false);
  protected readonly sending = signal(false);
  protected readonly shareUrl = signal('');
  protected validUntilLocal = this.toLocalDateTime(new Date(Date.now() + 14 * 86_400_000));
  protected draft: ProposalDraftRequest = {
    prospectId: this.route.snapshot.queryParamMap.get('prospectId'),
    clientId: null,
    eventId: null,
    currencyCode: 'MXN',
    validUntil: '',
    sharedIntroduction: 'Preparamos esta propuesta especialmente para tu evento.',
    sharedTerms: null,
    internalNotes: null,
    generalDiscountType: 'None',
    generalDiscountValue: 0,
    couponId: null,
    lines: [],
  };

  constructor() {
    this.loadInitialData();
  }

  protected addService(serviceId: string): void {
    const service = this.services().find((item) => item.id === serviceId);
    if (!service) {
      return;
    }
    this.draft.lines = [
      ...this.draft.lines,
      this.createLine(
        service.name,
        service.basePrice,
        service.id,
        null,
        service.taxBehavior === 'Exempt' ? 0 : 16,
      ),
    ];
  }

  protected addPackage(item: CatalogPackage): void {
    this.draft.lines = [
      ...this.draft.lines,
      this.createLine(item.name, item.basePrice, null, item.id, 16),
    ];
  }

  protected addCustomLine(): void {
    this.draft.lines = [
      ...this.draft.lines,
      this.createLine('Concepto personalizado', 0, null, null, 16),
    ];
  }

  protected removeLine(index: number): void {
    this.draft.lines = this.draft.lines
      .filter((_, itemIndex) => itemIndex !== index)
      .map((line, itemIndex) => ({ ...line, sortOrder: itemIndex }));
  }

  protected moveLine(index: number, offset: -1 | 1): void {
    const target = index + offset;
    if (target < 0 || target >= this.draft.lines.length) {
      return;
    }
    const lines = [...this.draft.lines];
    const currentLine = lines[index];
    const targetLine = lines[target];
    if (!currentLine || !targetLine) {
      return;
    }
    lines[index] = targetLine;
    lines[target] = currentLine;
    this.draft.lines = lines.map((line, itemIndex) => ({ ...line, sortOrder: itemIndex }));
  }

  protected lineTotal(line: ProposalDraftLineRequest): number {
    const subtotal = Math.max(0, line.quantity * line.unitPrice);
    const discount =
      line.discountType === 'Percentage'
        ? (subtotal * Math.min(100, Math.max(0, line.discountValue))) / 100
        : line.discountType === 'FixedAmount'
          ? Math.min(subtotal, Math.max(0, line.discountValue))
          : 0;
    const taxable = Math.max(0, subtotal - discount);
    return taxable + (taxable * Math.max(0, line.taxRate)) / 100;
  }

  protected previewSubtotal(): number {
    return this.requiredLines().reduce(
      (sum, line) => sum + Math.max(0, line.quantity * line.unitPrice),
      0,
    );
  }

  protected previewDiscount(): number {
    const lineDiscounts = this.requiredLines().reduce((sum, line) => {
      const subtotal = Math.max(0, line.quantity * line.unitPrice);
      return (
        sum +
        (line.discountType === 'Percentage'
          ? (subtotal * Math.min(100, line.discountValue)) / 100
          : line.discountType === 'FixedAmount'
            ? Math.min(subtotal, line.discountValue)
            : 0)
      );
    }, 0);
    const base = Math.max(0, this.previewSubtotal() - lineDiscounts);
    const general =
      this.draft.generalDiscountType === 'Percentage'
        ? (base * Math.min(100, this.draft.generalDiscountValue)) / 100
        : this.draft.generalDiscountType === 'FixedAmount'
          ? Math.min(base, this.draft.generalDiscountValue)
          : 0;
    const coupon = this.coupons().find((item) => item.id === this.draft.couponId);
    const afterGeneral = Math.max(0, base - general);
    const couponValue =
      coupon?.discountType === 'Percentage'
        ? (afterGeneral * Math.min(100, coupon.discountValue)) / 100
        : coupon?.discountType === 'FixedAmount'
          ? Math.min(afterGeneral, coupon.discountValue)
          : 0;
    return lineDiscounts + general + couponValue;
  }

  protected previewTax(): number {
    const included = this.requiredLines().map((line) => {
      const subtotal = Math.max(0, line.quantity * line.unitPrice);
      const lineDiscount =
        line.discountType === 'Percentage'
          ? (subtotal * Math.min(100, line.discountValue)) / 100
          : line.discountType === 'FixedAmount'
            ? Math.min(subtotal, line.discountValue)
            : 0;
      return {
        line,
        net: Math.max(0, subtotal - lineDiscount),
        lineDiscount,
      };
    });
    const netBeforeShared = included.reduce((sum, item) => sum + item.net, 0);
    const lineDiscountTotal = included.reduce((sum, item) => sum + item.lineDiscount, 0);
    const sharedDiscount = Math.max(0, this.previewDiscount() - lineDiscountTotal);
    return included.reduce((sum, item) => {
      const allocated = netBeforeShared > 0 ? (sharedDiscount * item.net) / netBeforeShared : 0;
      const taxable = Math.max(0, item.net - allocated);
      return sum + (taxable * Math.max(0, item.line.taxRate)) / 100;
    }, 0);
  }

  protected previewTotal(): number {
    return Math.max(0, this.previewSubtotal() - this.previewDiscount() + this.previewTax());
  }

  protected save(): void {
    if (!this.draft.prospectId && !this.draft.clientId) {
      this.toast.error('Selecciona un prospecto o cliente.');
      return;
    }
    if (!this.draft.lines.length || this.draft.lines.every((line) => line.isOptional)) {
      this.toast.error('Agrega al menos un concepto no opcional.');
      return;
    }
    this.saving.set(true);
    const request = this.buildRequest();
    const operation = this.proposalId
      ? this.api.updateProposalDraft(
          this.organization.requireOrganizationId(),
          this.proposalId,
          request,
        )
      : this.api.createProposal(this.organization.requireOrganizationId(), request);
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        this.saving.set(false);
        this.proposal.set(response);
        this.applyProposal(response);
        this.toast.success('Borrador guardado.');
        if (!this.proposalId) {
          void this.router.navigate(['/app/proposals', response.id], { replaceUrl: true });
        }
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.toast.error(getApiErrorMessage(error));
      },
    });
  }

  protected publish(): void {
    const current = this.proposal();
    if (!current) {
      return;
    }
    this.publishing.set(true);
    this.api
      .publishProposal(this.organization.requireOrganizationId(), current.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.publishing.set(false);
          this.toast.success('Versión inmutable publicada.');
          this.loadProposal(current.id);
        },
        error: (error: unknown) => {
          this.publishing.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected send(): void {
    const current = this.proposal();
    if (!current) {
      return;
    }
    this.sending.set(true);
    this.api
      .sendProposal(this.organization.requireOrganizationId(), current.id, null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.sending.set(false);
          this.shareUrl.set(response.shareUrl);
          this.toast.success('Enlace privado generado.');
          this.loadProposal(current.id);
        },
        error: (error: unknown) => {
          this.sending.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected canSend(proposal: ProposalResponse): boolean {
    return (
      proposal.currentVersionNumber > 0 && ['Ready', 'Sent', 'Viewed'].includes(proposal.status)
    );
  }

  protected copyLink(): void {
    void navigator.clipboard.writeText(this.shareUrl()).then(() => {
      this.toast.success('Enlace copiado.');
    });
  }

  protected downloadVersionPdf(versionId: string): void {
    const current = this.proposal();
    if (!current) {
      return;
    }
    this.api
      .downloadAdminProposalPdf(this.organization.requireOrganizationId(), current.id, versionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) =>
          this.downloadBlob(
            blob,
            `propuesta-${current.proposalNumber}-v${current.currentVersionNumber}.pdf`,
          ),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private loadInitialData(): void {
    const organizationId = this.organization.requireOrganizationId();
    forkJoin({
      prospects: this.api.getProspects(organizationId),
      clients: this.api.getClients(organizationId, '', 1, 100),
      services: this.api.getCatalogServices(organizationId),
      packages: this.api.getPackages(organizationId),
      coupons: this.api.getCoupons(organizationId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ prospects, clients, services, packages, coupons }) => {
          this.prospects.set(prospects.items);
          this.clients.set(clients.items);
          this.services.set(services);
          this.packages.set(packages);
          this.coupons.set(coupons);
          if (this.proposalId) {
            this.loadProposal(this.proposalId);
          } else {
            this.loading.set(false);
          }
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  private loadProposal(proposalId: string): void {
    this.api
      .getProposal(this.organization.requireOrganizationId(), proposalId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.proposal.set(response);
          this.applyProposal(response);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  private applyProposal(response: ProposalResponse): void {
    this.validUntilLocal = this.toLocalDateTime(new Date(response.validUntil));
    this.draft = {
      prospectId: response.prospectId,
      clientId: response.clientId,
      eventId: response.eventId,
      currencyCode: response.currencyCode,
      validUntil: response.validUntil,
      sharedIntroduction: response.sharedIntroduction,
      sharedTerms: response.sharedTerms,
      internalNotes: response.internalNotes,
      generalDiscountType: response.generalDiscountType,
      generalDiscountValue: response.generalDiscountValue,
      couponId: response.couponId,
      lines: response.draftLines.map((line) => ({
        description: line.description,
        serviceCatalogItemId: line.serviceCatalogItemId,
        packageId: line.packageId,
        quantity: line.quantity,
        unitPrice: line.unitPrice,
        discountType: line.discountType,
        discountValue: line.discountValue,
        taxRate: line.taxRate,
        isOptional: line.isOptional,
        sortOrder: line.sortOrder,
      })),
    };
  }

  private buildRequest(): ProposalDraftRequest {
    return {
      ...this.draft,
      validUntil: new Date(this.validUntilLocal).toISOString(),
      sharedIntroduction: this.draft.sharedIntroduction?.trim() || null,
      sharedTerms: this.draft.sharedTerms?.trim() || null,
      internalNotes: this.draft.internalNotes?.trim() || null,
      generalDiscountValue:
        this.draft.generalDiscountType === 'None'
          ? 0
          : Math.max(0, this.draft.generalDiscountValue),
      lines: this.draft.lines.map((line, index) => ({
        ...line,
        description: line.description.trim(),
        discountValue: line.discountType === 'None' ? 0 : Math.max(0, line.discountValue),
        sortOrder: index,
      })),
    };
  }

  private createLine(
    description: string,
    unitPrice: number,
    serviceCatalogItemId: string | null,
    packageId: string | null,
    taxRate: number,
  ): ProposalDraftLineRequest {
    return {
      description,
      serviceCatalogItemId,
      packageId,
      quantity: 1,
      unitPrice,
      discountType: 'None' as DiscountType,
      discountValue: 0,
      taxRate,
      isOptional: false,
      sortOrder: this.draft.lines.length,
    };
  }

  private requiredLines(): ProposalDraftLineRequest[] {
    return this.draft.lines.filter((line) => !line.isOptional);
  }

  private toLocalDateTime(date: Date): string {
    const offset = date.getTimezoneOffset() * 60_000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
}

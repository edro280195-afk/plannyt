import { CurrencyPipe, DatePipe } from '@angular/common';
import { A11yModule } from '@angular/cdk/a11y';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { forkJoin, Observable } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  CatalogPackage,
  Coupon,
  CouponRequest,
  PackageRequest,
  ServiceCatalogItem,
  ServiceCatalogItemRequest,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

type CatalogTab = 'services' | 'packages' | 'coupons';

@Component({
  selector: 'app-catalog-page',
  imports: [A11yModule, FormsModule, CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Oferta comercial</span>
          <h1>Catálogo</h1>
          <p>
            Precios de referencia para construir propuestas consistentes sin alterar históricos.
          </p>
        </div>
        @if (
          organization.hasPermission('catalog.manage') ||
          organization.hasPermission('packages.manage')
        ) {
          <button class="btn btn--primary" type="button" (click)="showEditor.set(true)">
            ＋ Agregar
          </button>
        }
      </header>

      <nav class="tabs" aria-label="Secciones del catálogo">
        <button
          [class.is-active]="tab() === 'services'"
          type="button"
          (click)="tab.set('services')"
        >
          Servicios <span>{{ services().length }}</span>
        </button>
        <button
          [class.is-active]="tab() === 'packages'"
          type="button"
          (click)="tab.set('packages')"
        >
          Paquetes <span>{{ packages().length }}</span>
        </button>
        <button [class.is-active]="tab() === 'coupons'" type="button" (click)="tab.set('coupons')">
          Cupones <span>{{ coupons().length }}</span>
        </button>
      </nav>

      @if (loading()) {
        <section class="card card--padded"><div class="skeleton skeleton--row"></div></section>
      } @else if (tab() === 'services') {
        <section class="catalog-grid">
          @for (service of services(); track service.id) {
            <article class="catalog-card card card--padded">
              <div class="catalog-card__topline">
                <span>{{ service.category }}</span>
                <span
                  class="status-chip"
                  [attr.data-status]="service.isActive ? 'Active' : 'Archived'"
                >
                  {{ service.isActive ? 'Activo' : 'Inactivo' }}
                </span>
              </div>
              <h2>{{ service.name }}</h2>
              <p>{{ service.description ?? 'Sin descripción' }}</p>
              <strong class="catalog-price">
                {{ service.basePrice | currency: service.currencyCode }}
                <small>{{ pricingLabel(service.pricingType) }}</small>
              </strong>
              <div class="catalog-meta">
                <span>{{ taxLabel(service.taxBehavior) }}</span>
                @if (service.isNegotiable) {
                  <span>Negociable</span>
                }
              </div>
            </article>
          } @empty {
            <div class="card card--padded empty-state"><h2>Aún no hay servicios</h2></div>
          }
        </section>
      } @else if (tab() === 'packages') {
        <section class="catalog-grid">
          @for (item of packages(); track item.id) {
            <article class="catalog-card card card--padded">
              <div class="catalog-card__topline">
                <span>Paquete</span><span>{{ item.items.length }} conceptos</span>
              </div>
              <h2>{{ item.name }}</h2>
              <p>{{ item.description ?? 'Sin descripción' }}</p>
              <strong class="catalog-price">{{
                item.basePrice | currency: item.currencyCode
              }}</strong>
              <ul class="clean-list">
                @for (line of item.items; track line.id) {
                  <li>
                    {{ line.quantity }} × {{ line.serviceName }}
                    @if (line.isOptional) {
                      <small>Opcional</small>
                    }
                  </li>
                }
              </ul>
            </article>
          } @empty {
            <div class="card card--padded empty-state"><h2>Aún no hay paquetes</h2></div>
          }
        </section>
      } @else {
        <section class="card">
          <div class="responsive-table">
            <table>
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Descuento</th>
                  <th>Vigencia</th>
                  <th>Usos</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                @for (coupon of coupons(); track coupon.id) {
                  <tr>
                    <td>
                      <strong>{{ coupon.code }}</strong
                      ><small class="table-subtitle">{{ coupon.description }}</small>
                    </td>
                    <td>
                      {{
                        coupon.discountType === 'Percentage'
                          ? coupon.discountValue + '%'
                          : (coupon.discountValue | currency: 'MXN')
                      }}
                    </td>
                    <td>
                      {{ coupon.startsAt | date: 'dd/MM/yy' }} —
                      {{ coupon.endsAt | date: 'dd/MM/yy' }}
                    </td>
                    <td>{{ coupon.currentUses }} / {{ coupon.maximumUses ?? '∞' }}</td>
                    <td>
                      <span
                        class="status-chip"
                        [attr.data-status]="coupon.isActive ? 'Active' : 'Archived'"
                        >{{ coupon.isActive ? 'Activo' : 'Inactivo' }}</span
                      >
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="5">
                      <div class="empty-state"><h2>Aún no hay cupones</h2></div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>
      }

      @if (showEditor()) {
        <div
          class="modal-layer"
          role="dialog"
          aria-modal="true"
          aria-labelledby="catalog-editor-title"
          (keydown.escape)="showEditor.set(false)"
        >
          <form
            class="modal card card--padded form-stack"
            cdkTrapFocus
            [cdkTrapFocusAutoCapture]="true"
            (ngSubmit)="saveCurrent()"
          >
            <div class="section-heading">
              <div>
                <span class="eyebrow">Nuevo registro</span>
                <h2 id="catalog-editor-title">{{ editorTitle() }}</h2>
              </div>
              <button
                class="icon-button"
                type="button"
                aria-label="Cerrar editor de catálogo"
                (click)="showEditor.set(false)"
              >
                ×
              </button>
            </div>
            @if (tab() === 'services') {
              <div class="form-grid">
                <label class="span-2"
                  >Nombre<input name="serviceName" [(ngModel)]="serviceDraft.name" required
                /></label>
                <label
                  >Categoría<input name="category" [(ngModel)]="serviceDraft.category" required
                /></label>
                <label
                  >Tipo de precio<select name="pricingType" [(ngModel)]="serviceDraft.pricingType">
                    <option value="Fixed">Fijo</option>
                    <option value="StartingAt">Desde</option>
                    <option value="PerUnit">Por unidad</option>
                    <option value="Custom">Personalizado</option>
                  </select></label
                >
                <label
                  >Precio base<input
                    name="basePrice"
                    type="number"
                    min="0"
                    step="0.01"
                    [(ngModel)]="serviceDraft.basePrice"
                /></label>
                <label
                  >Impuestos<select name="tax" [(ngModel)]="serviceDraft.taxBehavior">
                    <option value="Exclusive">Más impuestos</option>
                    <option value="Inclusive">Impuestos incluidos</option>
                    <option value="Exempt">Exento</option>
                  </select></label
                >
                <label class="span-2"
                  >Descripción<textarea
                    name="serviceDescription"
                    [(ngModel)]="serviceDraft.description"
                  ></textarea>
                </label>
                <label class="checkbox-row"
                  ><input
                    name="negotiable"
                    type="checkbox"
                    [(ngModel)]="serviceDraft.isNegotiable"
                  />
                  Permitir negociar precio</label
                >
              </div>
            } @else if (tab() === 'packages') {
              <div class="form-grid">
                <label class="span-2"
                  >Nombre<input name="packageName" [(ngModel)]="packageDraft.name" required
                /></label>
                <label
                  >Precio del paquete<input
                    name="packagePrice"
                    type="number"
                    min="0"
                    step="0.01"
                    [(ngModel)]="packageDraft.basePrice"
                /></label>
                <label class="checkbox-row"
                  ><input
                    name="packageNegotiable"
                    type="checkbox"
                    [(ngModel)]="packageDraft.isNegotiable"
                  />
                  Precio negociable</label
                >
                <label class="span-2"
                  >Descripción<textarea
                    name="packageDescription"
                    [(ngModel)]="packageDraft.description"
                  ></textarea>
                </label>
              </div>
              <fieldset class="fieldset">
                <legend>Servicios incluidos</legend>
                <div class="choice-list">
                  @for (service of services(); track service.id; let index = $index) {
                    <label class="choice-card">
                      <input
                        type="checkbox"
                        [name]="'packageService' + index"
                        [checked]="packageHas(service.id)"
                        (change)="togglePackageService(service)"
                      />
                      <span
                        ><strong>{{ service.name }}</strong
                        ><small>{{
                          service.basePrice | currency: service.currencyCode
                        }}</small></span
                      >
                    </label>
                  }
                </div>
              </fieldset>
            } @else {
              <div class="form-grid">
                <label
                  >Código<input name="couponCode" [(ngModel)]="couponDraft.code" required
                /></label>
                <label
                  >Tipo<select name="couponType" [(ngModel)]="couponDraft.discountType">
                    <option value="Percentage">Porcentaje</option>
                    <option value="FixedAmount">Monto fijo</option>
                  </select></label
                >
                <label
                  >Valor<input
                    name="couponValue"
                    type="number"
                    min="0"
                    step="0.01"
                    [(ngModel)]="couponDraft.discountValue"
                /></label>
                <label
                  >Máximo de usos<input
                    name="couponUses"
                    type="number"
                    min="1"
                    [(ngModel)]="couponDraft.maximumUses"
                /></label>
                <label
                  >Inicia<input
                    name="startsAt"
                    type="datetime-local"
                    [(ngModel)]="couponStartsAt"
                    required
                /></label>
                <label
                  >Termina<input
                    name="endsAt"
                    type="datetime-local"
                    [(ngModel)]="couponEndsAt"
                    required
                /></label>
                <label class="span-2"
                  >Descripción<input name="couponDescription" [(ngModel)]="couponDraft.description"
                /></label>
              </div>
            }
            <div class="form-actions">
              <button class="btn btn--quiet" type="button" (click)="showEditor.set(false)">
                Cancelar
              </button>
              <button class="btn btn--primary" type="submit" [disabled]="saving()">
                {{ saving() ? 'Guardando…' : 'Guardar' }}
              </button>
            </div>
          </form>
        </div>
      }
    </div>
  `,
})
export class CatalogPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly tab = signal<CatalogTab>('services');
  protected readonly services = signal<ServiceCatalogItem[]>([]);
  protected readonly packages = signal<CatalogPackage[]>([]);
  protected readonly coupons = signal<Coupon[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly showEditor = signal(false);

  protected serviceDraft: ServiceCatalogItemRequest = {
    name: '',
    description: null,
    category: '',
    pricingType: 'Fixed',
    basePrice: 0,
    currencyCode: 'MXN',
    taxBehavior: 'Exclusive',
    isNegotiable: false,
    isActive: true,
    sortOrder: 0,
  };
  protected packageDraft: PackageRequest = {
    name: '',
    description: null,
    basePrice: 0,
    currencyCode: 'MXN',
    isNegotiable: false,
    isActive: true,
    items: [],
  };
  protected couponDraft: CouponRequest = {
    code: '',
    description: null,
    discountType: 'Percentage',
    discountValue: 0,
    startsAt: '',
    endsAt: '',
    maximumUses: null,
    isActive: true,
  };
  protected couponStartsAt = '';
  protected couponEndsAt = '';

  constructor() {
    this.load();
  }

  protected editorTitle(): string {
    return this.tab() === 'services'
      ? 'Agregar servicio'
      : this.tab() === 'packages'
        ? 'Crear paquete'
        : 'Crear cupón';
  }

  protected pricingLabel(value: string): string {
    return (
      { Fixed: 'precio fijo', StartingAt: 'desde', PerUnit: 'por unidad', Custom: 'personalizado' }[
        value
      ] ?? value
    );
  }

  protected taxLabel(value: string): string {
    return (
      { Exclusive: 'Más impuestos', Inclusive: 'Impuestos incluidos', Exempt: 'Exento' }[value] ??
      value
    );
  }

  protected packageHas(serviceId: string): boolean {
    return this.packageDraft.items.some((item) => item.serviceCatalogItemId === serviceId);
  }

  protected togglePackageService(service: ServiceCatalogItem): void {
    if (this.packageHas(service.id)) {
      this.packageDraft.items = this.packageDraft.items.filter(
        (item) => item.serviceCatalogItemId !== service.id,
      );
      return;
    }
    this.packageDraft.items = [
      ...this.packageDraft.items,
      {
        serviceCatalogItemId: service.id,
        quantity: 1,
        isOptional: false,
        includedPrice: service.basePrice,
        sortOrder: this.packageDraft.items.length,
      },
    ];
  }

  protected saveCurrent(): void {
    if (this.saving()) {
      return;
    }
    this.saving.set(true);
    const organizationId = this.organization.requireOrganizationId();
    const request: Observable<unknown> =
      this.tab() === 'services'
        ? this.api.createCatalogService(organizationId, {
            ...this.serviceDraft,
            name: this.serviceDraft.name.trim(),
            category: this.serviceDraft.category.trim(),
            description: this.serviceDraft.description?.trim() || null,
          })
        : this.tab() === 'packages'
          ? this.api.createPackage(organizationId, {
              ...this.packageDraft,
              name: this.packageDraft.name.trim(),
              description: this.packageDraft.description?.trim() || null,
            })
          : this.api.createCoupon(organizationId, {
              ...this.couponDraft,
              code: this.couponDraft.code.trim().toUpperCase(),
              description: this.couponDraft.description?.trim() || null,
              startsAt: new Date(this.couponStartsAt).toISOString(),
              endsAt: new Date(this.couponEndsAt).toISOString(),
            });
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.showEditor.set(false);
        this.toast.success('Catálogo actualizado.');
        this.resetDrafts();
        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.toast.error(getApiErrorMessage(error));
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    const organizationId = this.organization.requireOrganizationId();
    forkJoin({
      services: this.api.getCatalogServices(organizationId),
      packages: this.api.getPackages(organizationId),
      coupons: this.api.getCoupons(organizationId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ services, packages, coupons }) => {
          this.services.set(services);
          this.packages.set(packages);
          this.coupons.set(coupons);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  private resetDrafts(): void {
    this.serviceDraft = {
      ...this.serviceDraft,
      name: '',
      description: null,
      category: '',
      basePrice: 0,
    };
    this.packageDraft = {
      ...this.packageDraft,
      name: '',
      description: null,
      basePrice: 0,
      items: [],
    };
    this.couponDraft = { ...this.couponDraft, code: '', description: null, discountValue: 0 };
  }
}

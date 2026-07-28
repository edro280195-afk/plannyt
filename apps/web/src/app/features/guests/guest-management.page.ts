import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  AgeCategory,
  EventGuest,
  GuestDashboard,
  GuestDuplicateSuggestion,
  GuestImportAnalysis,
  GuestImportResult,
  GuestTag,
  GuestType,
  InvitationGroup,
  InvitationGroupType,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-guest-management-page',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page guest-workspace">
      <a class="back-link" [routerLink]="['/app/events', eventId]">← Volver al evento</a>
      <header class="page-header">
        <div>
          <span class="eyebrow">Experiencia digital</span>
          <h1>Invitados y grupos</h1>
          <p>Organiza capacidades, contactos, etiquetas y enlaces privados.</p>
        </div>
        <div class="page-header__actions">
          <a class="btn btn--primary" [routerLink]="['/app/events', eventId, 'invitations']">
            Diseñar invitación
          </a>
          @if (organization.hasPermission('guests.export')) {
            <button class="btn btn--secondary" type="button" (click)="exportCsv()">
              Exportar CSV
            </button>
          }
        </div>
      </header>

      @if (dashboard(); as data) {
        <section class="guest-metrics" aria-label="Resumen de invitados">
          <article class="metric-card">
            <small>Invitados activos</small>
            <strong>{{ data.activeGuestCount }}</strong>
            <span>de {{ data.plan.limit }} en {{ planLabel(data.plan.tier) }}</span>
          </article>
          <article class="metric-card">
            <small>Grupos</small>
            <strong>{{ data.groupCount }}</strong>
            <span>{{ data.linkCount }} enlaces activos</span>
          </article>
          <article class="metric-card">
            <small>Enlaces abiertos</small>
            <strong>{{ data.openedLinkCount }}</strong>
            <span>métrica básica, sin rastreo invasivo</span>
          </article>
          <article class="metric-card metric-card--progress">
            <small>Uso del plan</small>
            <strong>{{ data.plan.percentage }}%</strong>
            <div class="progress-track" aria-hidden="true">
              <span [style.width.%]="data.plan.percentage"></span>
            </div>
          </article>
        </section>
        @if (data.plan.warning80) {
          <div
            class="notice"
            [class.notice--warning]="!data.plan.isAtLimit"
            [class.notice--error]="data.plan.isAtLimit"
          >
            <span>
              {{
                data.plan.isAtLimit
                  ? 'Alcanzaste el límite del plan; archiva invitados para liberar espacio.'
                  : 'Te acercas al límite de invitados activos de este evento.'
              }}
            </span>
          </div>
        }
      }

      <nav class="segmented-tabs" aria-label="Gestión de invitados">
        <button
          type="button"
          [class.is-active]="activeView() === 'guests'"
          (click)="activeView.set('guests')"
        >
          Invitados
        </button>
        <button
          type="button"
          [class.is-active]="activeView() === 'groups'"
          (click)="activeView.set('groups')"
        >
          Grupos
        </button>
        <button
          type="button"
          [class.is-active]="activeView() === 'import'"
          (click)="activeView.set('import')"
        >
          Importar CSV
        </button>
        <button
          type="button"
          [class.is-active]="activeView() === 'duplicates'"
          (click)="showDuplicates()"
        >
          Duplicados
        </button>
      </nav>

      @if (activeView() === 'guests') {
        <section class="split-layout">
          <article class="panel stack">
            <div class="guest-toolbar">
              <label class="search-field">
                <span class="sr-only">Buscar invitado</span>
                <input
                  type="search"
                  placeholder="Buscar nombre, correo o teléfono"
                  [formControl]="searchControl"
                  (keyup.enter)="load()"
                />
              </label>
              <select [formControl]="groupFilter" (change)="load()">
                <option value="">Todos los grupos</option>
                @for (group of dashboard()?.groups ?? []; track group.id) {
                  <option [value]="group.id">{{ group.displayName }}</option>
                }
              </select>
              <button class="btn btn--secondary" type="button" (click)="load()">Buscar</button>
            </div>
            <div class="guest-table" role="table" aria-label="Lista de invitados">
              @for (guest of dashboard()?.guests ?? []; track guest.id) {
                <div class="guest-row" role="row">
                  <span class="avatar avatar--soft">{{
                    guest.firstName.charAt(0) || guest.lastName.charAt(0)
                  }}</span>
                  <span>
                    <strong>{{ guest.firstName }} {{ guest.lastName }}</strong>
                    <small>
                      {{ groupName(guest.invitationGroupId) }}
                      @if (guest.isPrimaryContact) {
                        · Contacto principal
                      }
                    </small>
                  </span>
                  <span class="guest-badges">
                    @if (guest.isVip) {
                      <span class="tag-chip tag-chip--rose">VIP</span>
                    }
                    <span class="tag-chip">{{ ageLabel(guest.ageCategory) }}</span>
                  </span>
                  <span class="row-actions">
                    <button class="btn btn--quiet" type="button" (click)="editGuest(guest)">
                      Editar
                    </button>
                    @if (organization.hasPermission('guests.archive')) {
                      <button
                        class="icon-button icon-button--danger"
                        type="button"
                        aria-label="Archivar invitado"
                        (click)="archiveGuest(guest)"
                      >
                        ×
                      </button>
                    }
                  </span>
                </div>
              } @empty {
                <div class="empty-state">
                  <h3>Aún no hay invitados</h3>
                  <p>Agrega el primer invitado o importa tu lista desde CSV.</p>
                </div>
              }
            </div>
          </article>

          @if (organization.hasPermission(editingGuestId() ? 'guests.update' : 'guests.create')) {
            <aside class="panel">
              <span class="eyebrow">{{ editingGuestId() ? 'Edición' : 'Nuevo registro' }}</span>
              <h2>{{ editingGuestId() ? 'Editar invitado' : 'Agregar invitado' }}</h2>
              <form [formGroup]="guestForm" (ngSubmit)="saveGuest()" class="form-stack">
                <label
                  >Grupo
                  <select formControlName="invitationGroupId">
                    <option value="">Sin grupo</option>
                    @for (group of dashboard()?.groups ?? []; track group.id) {
                      <option [value]="group.id">
                        {{ group.displayName }} · {{ group.availableGuestCount }} lugares
                      </option>
                    }
                  </select>
                </label>
                <div class="form-grid">
                  <label>Nombre <input formControlName="firstName" /></label>
                  <label>Apellido <input formControlName="lastName" /></label>
                  <label>Correo <input type="email" formControlName="email" /></label>
                  <label>Teléfono <input formControlName="phone" /></label>
                  <label
                    >Tipo
                    <select formControlName="guestType">
                      @for (type of guestTypes; track type) {
                        <option [value]="type">{{ guestTypeLabel(type) }}</option>
                      }
                    </select>
                  </label>
                  <label
                    >Edad
                    <select formControlName="ageCategory">
                      @for (age of ageCategories; track age) {
                        <option [value]="age">{{ ageLabel(age) }}</option>
                      }
                    </select>
                  </label>
                </div>
                <label class="check-line"
                  ><input type="checkbox" formControlName="isPrimaryContact" />
                  <span>Contacto principal del grupo</span></label
                >
                <label class="check-line"
                  ><input type="checkbox" formControlName="isVip" />
                  <span>Invitado VIP</span></label
                >
                <div class="form-actions">
                  <button
                    class="btn btn--primary"
                    type="submit"
                    [disabled]="guestForm.invalid || saving()"
                  >
                    {{ saving() ? 'Guardando…' : 'Guardar invitado' }}
                  </button>
                  @if (editingGuestId()) {
                    <button class="btn btn--quiet" type="button" (click)="resetGuestForm()">
                      Cancelar
                    </button>
                  }
                </div>
              </form>
            </aside>
          }
        </section>
      }

      @if (activeView() === 'groups') {
        <section class="split-layout">
          <article class="panel stack">
            @for (group of dashboard()?.groups ?? []; track group.id) {
              <div class="group-card" [class.group-card--selected]="editingGroupId() === group.id">
                <div class="group-card__head">
                  <div>
                    <span class="status-chip" [attr.data-status]="group.status">{{
                      groupStatusLabel(group.status)
                    }}</span>
                    <h3>{{ group.displayName }}</h3>
                  </div>
                  <strong>{{ group.namedGuestCount }}/{{ group.allowedGuestCount }}</strong>
                </div>
                <div class="capacity-track">
                  <span [style.width.%]="capacityPercentage(group)"></span>
                </div>
                <p>
                  {{ group.availableGuestCount }} lugares disponibles · origen {{ group.source }}
                </p>
                <div class="tag-list">
                  @for (tag of group.tags; track tag.id) {
                    <span class="tag-chip" [attr.data-color]="tag.colorToken">{{ tag.name }}</span>
                  }
                </div>
                <div class="row-actions">
                  <button class="btn btn--quiet" type="button" (click)="editGroup(group)">
                    Editar
                  </button>
                  @if (organization.hasPermission('invitation-groups.archive')) {
                    <button
                      class="btn btn--quiet danger-text"
                      type="button"
                      (click)="archiveGroup(group)"
                    >
                      Archivar
                    </button>
                  }
                </div>
              </div>
            } @empty {
              <div class="empty-state">
                <h3>Sin grupos</h3>
                <p>Crea grupos para controlar capacidad y enlaces.</p>
              </div>
            }
          </article>
          <aside class="panel stack">
            <div>
              <span class="eyebrow">{{ editingGroupId() ? 'Edición' : 'Nuevo grupo' }}</span>
              <h2>{{ editingGroupId() ? 'Editar grupo' : 'Crear grupo de invitación' }}</h2>
            </div>
            <form [formGroup]="groupForm" (ngSubmit)="saveGroup()" class="form-stack">
              <label>Nombre visible <input formControlName="displayName" /></label>
              <label
                >Tipo
                <select formControlName="groupType">
                  @for (type of groupTypes; track type) {
                    <option [value]="type">{{ groupTypeLabel(type) }}</option>
                  }
                </select>
              </label>
              <div class="form-grid">
                <label
                  >Capacidad <input type="number" min="1" formControlName="allowedGuestCount"
                /></label>
                <label
                  >Máximo sin nombre
                  <input type="number" min="0" formControlName="maxUnnamedCompanions"
                /></label>
              </div>
              <label class="check-line"
                ><input type="checkbox" formControlName="allowUnnamedCompanions" />
                <span>Permitir acompañantes sin nombre</span></label
              >
              <label>Contacto <input formControlName="contactName" /></label>
              <div class="form-grid">
                <label>Teléfono <input formControlName="contactPhone" /></label>
                <label>Correo <input type="email" formControlName="contactEmail" /></label>
              </div>
              <fieldset class="tag-picker">
                <legend>Etiquetas</legend>
                @for (tag of tags(); track tag.id) {
                  <label class="check-line">
                    <input
                      type="checkbox"
                      [checked]="selectedTagIds().includes(tag.id)"
                      (change)="toggleTag(tag.id)"
                    />
                    <span>{{ tag.name }}</span>
                  </label>
                }
              </fieldset>
              <label
                >Notas internas <textarea rows="3" formControlName="internalNotes"></textarea>
              </label>
              <div class="form-actions">
                <button
                  class="btn btn--primary"
                  type="submit"
                  [disabled]="groupForm.invalid || saving()"
                >
                  Guardar grupo
                </button>
                @if (editingGroupId()) {
                  <button class="btn btn--quiet" type="button" (click)="resetGroupForm()">
                    Cancelar
                  </button>
                }
              </div>
            </form>
            @if (organization.hasPermission('guests.manage-tags')) {
              <form [formGroup]="tagForm" (ngSubmit)="saveTag()" class="inline-form">
                <label
                  >{{ editingTagId() ? 'Editar etiqueta' : 'Nueva etiqueta' }}
                  <input formControlName="name" placeholder="Ej. Familia"
                /></label>
                <label
                  >Color
                  <select formControlName="colorToken">
                    @for (color of colorTokens; track color) {
                      <option [value]="color">{{ color }}</option>
                    }
                  </select>
                </label>
                <button class="btn btn--secondary" type="submit">
                  {{ editingTagId() ? 'Guardar' : 'Agregar' }}
                </button>
                @if (editingTagId()) {
                  <button class="btn btn--quiet" type="button" (click)="resetTagForm()">
                    Cancelar
                  </button>
                }
              </form>
              <div class="tag-list">
                @for (tag of tags(); track tag.id) {
                  <span class="tag-chip" [attr.data-color]="tag.colorToken">{{ tag.name }}</span>
                  <button
                    class="icon-button"
                    type="button"
                    [attr.aria-label]="'Editar ' + tag.name"
                    (click)="editTag(tag)"
                  >
                    ✎
                  </button>
                  <button
                    class="icon-button"
                    type="button"
                    [attr.aria-label]="'Archivar ' + tag.name"
                    (click)="archiveTag(tag)"
                  >
                    ×
                  </button>
                }
              </div>
            }
          </aside>
        </section>
      }

      @if (activeView() === 'import') {
        <section class="panel import-panel">
          <div>
            <span class="eyebrow">Hasta 5,000 filas</span>
            <h2>Importación CSV segura</h2>
            <p>Analiza encabezados, valida cada fila y confirma en una sola transacción.</p>
          </div>
          <div class="import-actions">
            <button class="btn btn--secondary" type="button" (click)="downloadTemplate()">
              Descargar plantilla
            </button>
            <label class="file-button btn btn--primary">
              Seleccionar CSV
              <input type="file" accept=".csv,text/csv" (change)="analyzeFile($event)" />
            </label>
          </div>
          @if (importAnalysis(); as analysis) {
            <div class="import-summary">
              <span
                ><strong>{{ analysis.totalRows }}</strong
                ><small>filas</small></span
              >
              <span
                ><strong>{{ analysis.validRows }}</strong
                ><small>válidas</small></span
              >
              <span [class.danger-text]="analysis.errorRows > 0"
                ><strong>{{ analysis.errorRows }}</strong
                ><small>con error</small></span
              >
            </div>
            <div class="preview-table">
              @for (row of analysis.preview; track row.rowNumber) {
                <div class="preview-row" [class.preview-row--error]="!row.isValid">
                  <strong>#{{ row.rowNumber }}</strong>
                  <span>{{ row.groupName || 'Sin grupo' }}</span>
                  <span>{{ row.guestName || 'Sin nombre' }}</span>
                  <span>{{ row.isValid ? 'Lista' : row.errors.join(' · ') }}</span>
                </div>
              }
            </div>
            <button
              class="btn btn--primary"
              type="button"
              [disabled]="analysis.errorRows > 0 || saving()"
              (click)="confirmImport()"
            >
              Confirmar importación
            </button>
            @if (analysis.errorRows > 0) {
              <p class="danger-text">
                Corrige las filas marcadas antes de confirmar; ninguna se omitirá silenciosamente.
              </p>
            }
          }
          @if (importResult(); as result) {
            <div class="notice notice--success">
              <span
                >Se crearon {{ result.createdGuests }} invitados en
                {{ result.createdGroups }} grupos.</span
              >
            </div>
          }
        </section>
      }

      @if (activeView() === 'duplicates') {
        <section class="panel">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Sugerencias</span>
              <h2>Posibles duplicados</h2>
            </div>
            <button class="btn btn--secondary" type="button" (click)="showDuplicates()">
              Actualizar
            </button>
          </div>
          @for (duplicate of duplicates(); track duplicate.kind + duplicate.guestIds.join('-')) {
            <div class="duplicate-row">
              <span class="status-chip">{{ duplicate.reason }}</span>
              <strong>{{ duplicate.guestIds.length }} registros relacionados</strong>
              <p>{{ duplicate.suggestedAction }}</p>
              <small>No se realizará ninguna fusión automática.</small>
            </div>
          } @empty {
            <div class="empty-state">
              <h3>Sin coincidencias</h3>
              <p>No encontramos correos, teléfonos o nombres repetidos.</p>
            </div>
          }
        </section>
      }
    </div>
  `,
})
export class GuestManagementPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly eventId = this.route.snapshot.paramMap.get('id') ?? '';
  private readonly organizationId = this.organization.requireOrganizationId();
  protected readonly dashboard = signal<GuestDashboard | null>(null);
  protected readonly tags = signal<GuestTag[]>([]);
  protected readonly duplicates = signal<GuestDuplicateSuggestion[]>([]);
  protected readonly importAnalysis = signal<GuestImportAnalysis | null>(null);
  protected readonly importResult = signal<GuestImportResult | null>(null);
  protected readonly saving = signal(false);
  protected readonly activeView = signal<'guests' | 'groups' | 'import' | 'duplicates'>('guests');
  protected readonly editingGuestId = signal<string | null>(null);
  protected readonly editingGroupId = signal<string | null>(null);
  protected readonly selectedTagIds = signal<string[]>([]);
  protected readonly editingTagId = signal<string | null>(null);
  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly groupFilter = new FormControl('', { nonNullable: true });
  protected readonly guestTypes: GuestType[] = [
    'Standard',
    'Family',
    'Friend',
    'Colleague',
    'Vendor',
    'WeddingParty',
    'SponsorOrGodparent',
    'StaffGuest',
    'VendorGuest',
    'Vip',
    'Other',
  ];
  protected readonly ageCategories: AgeCategory[] = ['Adult', 'Teen', 'Child', 'Infant', 'Unknown'];
  protected readonly groupTypes: InvitationGroupType[] = [
    'Individual',
    'Couple',
    'Family',
    'Group',
    'Company',
    'CorporateTable',
    'Other',
  ];
  protected readonly colorTokens = ['slate', 'rose', 'amber', 'emerald', 'sky', 'indigo', 'violet'];
  protected readonly guestForm = new FormGroup({
    invitationGroupId: new FormControl('', { nonNullable: true }),
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(100)] }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(100)] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
    phone: new FormControl('', { nonNullable: true }),
    guestType: new FormControl<GuestType>('Other', { nonNullable: true }),
    ageCategory: new FormControl<AgeCategory>('Adult', { nonNullable: true }),
    isPrimaryContact: new FormControl(false, { nonNullable: true }),
    isVip: new FormControl(false, { nonNullable: true }),
  });
  protected readonly groupForm = new FormGroup({
    displayName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(160)],
    }),
    groupType: new FormControl<InvitationGroupType>('Family', { nonNullable: true }),
    allowedGuestCount: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)],
    }),
    allowUnnamedCompanions: new FormControl(false, { nonNullable: true }),
    maxUnnamedCompanions: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)],
    }),
    contactName: new FormControl('', { nonNullable: true }),
    contactPhone: new FormControl('', { nonNullable: true }),
    contactEmail: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
    internalNotes: new FormControl('', { nonNullable: true }),
  });
  protected readonly tagForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(60)],
    }),
    colorToken: new FormControl('slate', { nonNullable: true }),
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    forkJoin({
      dashboard: this.api.getGuestDashboard(this.organizationId, this.eventId, {
        search: this.searchControl.value,
        groupId: this.groupFilter.value,
      }),
      tags: this.api.getGuestTags(this.organizationId, this.eventId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ dashboard, tags }) => {
          this.dashboard.set(dashboard);
          this.tags.set(tags);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected saveGuest(): void {
    if (this.guestForm.invalid || this.saving()) return;
    const value = this.guestForm.getRawValue();
    if (!value.firstName.trim() && !value.lastName.trim()) {
      this.toast.error('Escribe el nombre o apellido del invitado.');
      return;
    }
    const request = {
      invitationGroupId: value.invitationGroupId || null,
      personId: null,
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      email: value.email.trim() || null,
      phone: value.phone.trim() || null,
      guestType: value.guestType,
      ageCategory: value.ageCategory,
      isPrimaryContact: value.isPrimaryContact,
      isNamed: true,
      isPlusOne: false,
      isVip: value.isVip,
      sortOrder: 0,
      internalNotes: null,
    };
    const operation = this.editingGuestId()
      ? this.api.updateEventGuest(
          this.organizationId,
          this.eventId,
          this.editingGuestId()!,
          request,
        )
      : this.api.createEventGuest(this.organizationId, this.eventId, request);
    this.saving.set(true);
    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.toast.success(this.editingGuestId() ? 'Invitado actualizado.' : 'Invitado agregado.');
        this.resetGuestForm();
        this.load();
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected editGuest(guest: EventGuest): void {
    this.editingGuestId.set(guest.id);
    this.guestForm.setValue({
      invitationGroupId: guest.invitationGroupId ?? '',
      firstName: guest.firstName,
      lastName: guest.lastName,
      email: guest.email ?? '',
      phone: guest.phone ?? '',
      guestType: guest.guestType,
      ageCategory: guest.ageCategory,
      isPrimaryContact: guest.isPrimaryContact,
      isVip: guest.isVip,
    });
  }

  protected resetGuestForm(): void {
    this.editingGuestId.set(null);
    this.guestForm.reset({
      invitationGroupId: '',
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      guestType: 'Other',
      ageCategory: 'Adult',
      isPrimaryContact: false,
      isVip: false,
    });
  }

  protected archiveGuest(guest: EventGuest): void {
    if (!confirm(`¿Archivar a ${guest.firstName} ${guest.lastName}?`)) return;
    this.api.archiveEventGuest(this.organizationId, this.eventId, guest.id).subscribe({
      next: () => {
        this.toast.success('Invitado archivado.');
        this.load();
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected saveGroup(): void {
    if (this.groupForm.invalid || this.saving()) return;
    const value = this.groupForm.getRawValue();
    const request = {
      groupType: value.groupType,
      displayName: value.displayName.trim(),
      contactName: value.contactName.trim() || null,
      contactPhone: value.contactPhone.trim() || null,
      contactEmail: value.contactEmail.trim() || null,
      allowedGuestCount: value.allowedGuestCount,
      allowUnnamedCompanions: value.allowUnnamedCompanions,
      maxUnnamedCompanions: value.allowUnnamedCompanions ? value.maxUnnamedCompanions : 0,
      internalNotes: value.internalNotes.trim() || null,
      tagIds: this.selectedTagIds(),
      applyCapacityOverride: false,
    };
    const operation = this.editingGroupId()
      ? this.api.updateInvitationGroup(
          this.organizationId,
          this.eventId,
          this.editingGroupId()!,
          request,
        )
      : this.api.createInvitationGroup(this.organizationId, this.eventId, request);
    this.saving.set(true);
    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.toast.success(this.editingGroupId() ? 'Grupo actualizado.' : 'Grupo creado.');
        this.resetGroupForm();
        this.load();
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected editGroup(group: InvitationGroup): void {
    this.editingGroupId.set(group.id);
    this.selectedTagIds.set(group.tags.map((tag) => tag.id));
    this.groupForm.setValue({
      displayName: group.displayName,
      groupType: group.groupType,
      allowedGuestCount: group.allowedGuestCount,
      allowUnnamedCompanions: group.allowUnnamedCompanions,
      maxUnnamedCompanions: group.maxUnnamedCompanions,
      contactName: group.contactName ?? '',
      contactPhone: group.contactPhone ?? '',
      contactEmail: group.contactEmail ?? '',
      internalNotes: group.internalNotes ?? '',
    });
  }

  protected resetGroupForm(): void {
    this.editingGroupId.set(null);
    this.selectedTagIds.set([]);
    this.groupForm.reset({
      displayName: '',
      groupType: 'Family',
      allowedGuestCount: 1,
      allowUnnamedCompanions: false,
      maxUnnamedCompanions: 0,
      contactName: '',
      contactPhone: '',
      contactEmail: '',
      internalNotes: '',
    });
  }

  protected archiveGroup(group: InvitationGroup): void {
    if (!confirm(`¿Archivar el grupo ${group.displayName} y sus invitados?`)) return;
    this.api.archiveInvitationGroup(this.organizationId, this.eventId, group.id).subscribe({
      next: () => {
        this.toast.success('Grupo archivado.');
        this.load();
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected toggleTag(tagId: string): void {
    this.selectedTagIds.update((ids) =>
      ids.includes(tagId) ? ids.filter((id) => id !== tagId) : [...ids, tagId],
    );
  }

  protected saveTag(): void {
    if (this.tagForm.invalid) return;
    const tagId = this.editingTagId();
    const operation = tagId
      ? this.api.updateGuestTag(
          this.organizationId,
          this.eventId,
          tagId,
          this.tagForm.getRawValue(),
        )
      : this.api.createGuestTag(this.organizationId, this.eventId, this.tagForm.getRawValue());
    operation.subscribe({
      next: (tag) => {
        this.tags.update((tags) =>
          tagId ? tags.map((item) => (item.id === tag.id ? tag : item)) : [...tags, tag],
        );
        this.resetTagForm();
        this.toast.success(tagId ? 'Etiqueta actualizada.' : 'Etiqueta creada.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected editTag(tag: GuestTag): void {
    this.editingTagId.set(tag.id);
    this.tagForm.reset({ name: tag.name, colorToken: tag.colorToken });
  }

  protected resetTagForm(): void {
    this.editingTagId.set(null);
    this.tagForm.reset({ name: '', colorToken: 'slate' });
  }

  protected archiveTag(tag: GuestTag): void {
    if (!confirm(`¿Archivar la etiqueta ${tag.name}?`)) return;
    this.api.archiveGuestTag(this.organizationId, this.eventId, tag.id).subscribe({
      next: () => {
        this.tags.update((tags) => tags.filter((item) => item.id !== tag.id));
        this.selectedTagIds.update((ids) => ids.filter((id) => id !== tag.id));
        this.toast.success('Etiqueta archivada.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected showDuplicates(): void {
    this.activeView.set('duplicates');
    this.api.getGuestDuplicates(this.organizationId, this.eventId).subscribe({
      next: (duplicates) => this.duplicates.set(duplicates),
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected analyzeFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.importResult.set(null);
    this.api.analyzeGuestImport(this.organizationId, this.eventId, file).subscribe({
      next: (analysis) => this.importAnalysis.set(analysis),
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected confirmImport(): void {
    const analysis = this.importAnalysis();
    if (!analysis || analysis.errorRows > 0 || this.saving()) return;
    this.saving.set(true);
    this.api
      .confirmGuestImport(this.organizationId, this.eventId, analysis.importId)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (result) => {
          this.importResult.set(result);
          this.toast.success('Importación completada.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected downloadTemplate(): void {
    this.api.downloadGuestTemplate(this.organizationId, this.eventId).subscribe({
      next: (blob) => this.download(blob, 'plantilla-invitados.csv'),
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected exportCsv(): void {
    this.api.exportGuests(this.organizationId, this.eventId).subscribe({
      next: (blob) => this.download(blob, `invitados-${this.eventId}.csv`),
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected groupName(groupId: string | null): string {
    return (
      this.dashboard()?.groups.find((group) => group.id === groupId)?.displayName ?? 'Sin grupo'
    );
  }

  protected capacityPercentage(group: InvitationGroup): number {
    return Math.min(100, Math.round((group.namedGuestCount * 100) / group.allowedGuestCount));
  }

  protected planLabel(tier: string): string {
    return (
      (
        {
          Community: 'Community',
          EventComplete: 'Evento Completo',
          PlannerPro: 'Planner Pro',
        } as Record<string, string>
      )[tier] ?? tier
    );
  }

  protected ageLabel(age: AgeCategory): string {
    return (
      {
        Adult: 'Adulto',
        Teen: 'Adolescente',
        Child: 'Niño',
        Infant: 'Bebé',
        Unknown: 'Sin especificar',
      } as Record<AgeCategory, string>
    )[age];
  }

  protected guestTypeLabel(type: GuestType): string {
    return (
      {
        Standard: 'Estándar',
        Family: 'Familia',
        Friend: 'Amistad',
        Colleague: 'Colega',
        Vendor: 'Proveedor',
        WeddingParty: 'Cortejo',
        SponsorOrGodparent: 'Padrino o madrina',
        StaffGuest: 'Invitado de staff',
        VendorGuest: 'Invitado de proveedor',
        Vip: 'VIP',
        Other: 'Otro',
      } as Record<GuestType, string>
    )[type];
  }

  protected groupTypeLabel(type: InvitationGroupType): string {
    return (
      {
        Individual: 'Individual',
        Couple: 'Pareja',
        Family: 'Familia',
        Company: 'Empresa',
        Other: 'Otro',
      } as Record<InvitationGroupType, string>
    )[type];
  }

  protected groupStatusLabel(status: string): string {
    return (
      (
        {
          Draft: 'Borrador',
          Ready: 'Listo',
          LinkGenerated: 'Enlace generado',
          SharedManually: 'Compartido',
          Opened: 'Abierto',
          Revoked: 'Revocado',
        } as Record<string, string>
      )[status] ?? status
    );
  }

  private download(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }
}

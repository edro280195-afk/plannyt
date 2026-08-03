import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  GUEST_IMPORT_FORMAT_OPTIONS,
  GUEST_IMPORT_LANGUAGE_OPTIONS,
  GuestImportFieldGuideEntry,
  guestImportFieldGuide,
  templateFileName,
} from '../../core/guests/guest-import-guide';
import {
  AgeCategory,
  GuestAccessLink,
  GuestDuplicateSuggestion,
  GuestImportAnalysis,
  GuestImportResult,
  GuestImportTemplateFormat,
  GuestImportTemplateLanguage,
  GuestType,
  InvitationGroupType,
  PortalGuest,
  PortalGuestWorkspace,
  PortalInvitationGroup,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-portal-guest-experience-page',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <a class="back-link" [routerLink]="['/portal/events', eventId]">← Volver al evento</a>
      <header class="page-header">
        <div>
          <span class="eyebrow">Colaboración</span>
          <h1>Invitados e invitación digital</h1>
          <p>Administra los datos compartidos y aprueba exactamente la versión presentada.</p>
        </div>
      </header>

      <nav class="segmented-tabs">
        <button type="button" [class.is-active]="tab() === 'list'" (click)="tab.set('list')">
          Lista
        </button>
        <button type="button" [class.is-active]="tab() === 'design'" (click)="tab.set('design')">
          Diseño
        </button>
        <button type="button" [class.is-active]="tab() === 'import'" (click)="tab.set('import')">
          Importar
        </button>
        <button type="button" [class.is-active]="tab() === 'links'" (click)="openLinks()">
          Enlaces
        </button>
      </nav>

      @if (tab() === 'list') {
        <section class="split-layout">
          <article class="panel stack">
            @for (group of workspace()?.groups ?? []; track group.id) {
              <div class="group-card">
                <div class="group-card__head">
                  <div>
                    <span class="eyebrow">Grupo</span>
                    <h3>{{ group.displayName }}</h3>
                  </div>
                  <div class="button-row">
                    <strong>{{ group.namedGuestCount }}/{{ group.allowedGuestCount }}</strong>
                    <button
                      class="icon-button"
                      type="button"
                      aria-label="Editar grupo"
                      (click)="editGroup(group)"
                    >
                      ✎
                    </button>
                    <button
                      class="icon-button"
                      type="button"
                      aria-label="Archivar grupo"
                      (click)="archiveGroup(group)"
                    >
                      ×
                    </button>
                  </div>
                </div>
                @for (guest of guestsFor(group.id); track guest.id) {
                  <div class="contact-row">
                    <span class="avatar avatar--soft">{{ guest.firstName.charAt(0) }}</span>
                    <span>
                      <strong>{{ guest.firstName }} {{ guest.lastName }}</strong>
                      <small>{{
                        guest.isPrimaryContact ? 'Contacto principal' : ageLabel(guest.ageCategory)
                      }}</small>
                    </span>
                    @if (guest.isVip) {
                      <span class="tag-chip tag-chip--rose">VIP</span>
                    }
                    <button
                      class="icon-button"
                      type="button"
                      aria-label="Editar invitado"
                      (click)="editGuest(guest)"
                    >
                      ✎
                    </button>
                    <button
                      class="icon-button"
                      type="button"
                      aria-label="Archivar invitado"
                      (click)="archiveGuest(guest)"
                    >
                      ×
                    </button>
                  </div>
                }
              </div>
            } @empty {
              <div class="empty-state">
                <h3>Sin grupos compartidos</h3>
                <p>Agrega el primero desde el formulario.</p>
              </div>
            }
          </article>

          <aside class="panel stack">
            <div>
              <span class="eyebrow">Colaborar</span>
              <h2>Agregar o editar datos</h2>
            </div>
            <form [formGroup]="groupForm" (ngSubmit)="saveGroup()" class="form-stack">
              <h3>{{ editingGroupId() ? 'Editar grupo' : 'Nuevo grupo' }}</h3>
              <label>Nombre <input formControlName="displayName" /></label>
              <div class="form-grid">
                <label
                  >Tipo
                  <select formControlName="groupType">
                    @for (type of groupTypes; track type) {
                      <option [value]="type">{{ type }}</option>
                    }
                  </select>
                </label>
                <label
                  >Capacidad <input type="number" min="1" formControlName="allowedGuestCount"
                /></label>
              </div>
              <label class="check-line">
                <input type="checkbox" formControlName="allowUnnamedCompanions" />
                <span>Permitir acompañantes sin nombre</span>
              </label>
              @if (groupForm.controls.allowUnnamedCompanions.value) {
                <label
                  >Máximo sin nombre
                  <input type="number" min="0" formControlName="maxUnnamedCompanions"
                /></label>
              }
              <div class="button-row">
                <button class="btn btn--secondary" type="submit">
                  {{ editingGroupId() ? 'Guardar grupo' : 'Crear grupo' }}
                </button>
                @if (editingGroupId()) {
                  <button class="btn btn--ghost" type="button" (click)="cancelGroupEdit()">
                    Cancelar
                  </button>
                }
              </div>
            </form>

            <form [formGroup]="guestForm" (ngSubmit)="saveGuest()" class="form-stack">
              <h3>{{ editingGuestId() ? 'Editar invitado' : 'Nuevo invitado' }}</h3>
              <label
                >Grupo
                <select formControlName="invitationGroupId">
                  <option value="">Selecciona</option>
                  @for (group of workspace()?.groups ?? []; track group.id) {
                    <option [value]="group.id">{{ group.displayName }}</option>
                  }
                </select>
              </label>
              <div class="form-grid">
                <label>Nombre <input formControlName="firstName" /></label>
                <label>Apellido <input formControlName="lastName" /></label>
              </div>
              <div class="form-grid">
                <label
                  >Relación
                  <select formControlName="guestType">
                    @for (type of guestTypes; track type) {
                      <option [value]="type">{{ type }}</option>
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
                ><input type="checkbox" formControlName="isPrimaryContact" /><span
                  >Contacto principal</span
                ></label
              >
              <label class="check-line"
                ><input type="checkbox" formControlName="isVip" /><span>VIP</span></label
              >
              <div class="button-row">
                <button class="btn btn--primary" type="submit">
                  {{ editingGuestId() ? 'Guardar invitado' : 'Agregar invitado' }}
                </button>
                @if (editingGuestId()) {
                  <button class="btn btn--ghost" type="button" (click)="cancelGuestEdit()">
                    Cancelar
                  </button>
                }
              </div>
            </form>
          </aside>
        </section>

        @if (duplicates().length > 0) {
          <section class="panel stack">
            <div>
              <span class="eyebrow">Revisión</span>
              <h2>Posibles duplicados</h2>
              <p>Son sugerencias; Plannyt no fusiona registros automáticamente.</p>
            </div>
            @for (duplicate of duplicates(); track duplicate.kind + duplicate.guestIds.join('-')) {
              <div class="notice notice--warning">
                <strong>{{ duplicate.reason }}</strong>
                <span
                  >{{ duplicate.guestIds.length }} registros · {{ duplicate.suggestedAction }}</span
                >
              </div>
            }
          </section>
        }
      }

      @if (tab() === 'design') {
        @if (workspace()?.design; as design) {
          <section class="portal-detail-grid">
            <article
              class="panel invitation-mini-preview"
              [style.background]="design.theme.backgroundColor"
              [style.color]="design.theme.textColor"
            >
              @for (block of design.blocks; track block.id) {
                @if (block.visible) {
                  <section class="invite-block">
                    <h2>{{ blockText(block, 'title') || blockText(block, 'heading') }}</h2>
                    <p>
                      {{
                        blockText(block, 'subtitle') ||
                          blockText(block, 'body') ||
                          blockText(block, 'details') ||
                          blockText(block, 'text')
                      }}
                    </p>
                  </section>
                }
              }
            </article>
            <aside class="panel stack">
              <div>
                <span class="status-chip" [attr.data-status]="design.status">{{
                  design.status
                }}</span>
                <h2>Revisión de versión</h2>
                <p>Una edición posterior requerirá una nueva aprobación.</p>
              </div>
              @if (design.versions[0]; as version) {
                <strong>Versión {{ version.versionNumber }}</strong>
                <form [formGroup]="reviewForm" class="form-stack">
                  <label>Comentario <textarea rows="4" formControlName="message"></textarea></label>
                  <button
                    class="btn btn--secondary"
                    type="button"
                    (click)="review(version.id, 'comments')"
                  >
                    Comentar
                  </button>
                  @if (design.status === 'InReview') {
                    <div class="button-row">
                      <button
                        class="btn btn--primary"
                        type="button"
                        (click)="review(version.id, 'approve')"
                      >
                        Aprobar versión
                      </button>
                      <button
                        class="btn btn--secondary"
                        type="button"
                        (click)="review(version.id, 'request-changes')"
                      >
                        Solicitar cambios
                      </button>
                    </div>
                  }
                </form>
              } @else {
                <div class="notice">La organización todavía no envía una versión a revisión.</div>
              }
              @for (comment of design.comments; track comment.id) {
                <div class="comment">
                  <strong>{{ comment.decision }}</strong>
                  <p>{{ comment.message }}</p>
                </div>
              }
            </aside>
          </section>
        } @else {
          <section class="panel empty-state">
            <h2>Aún no hay diseño</h2>
            <p>La organización compartirá aquí la primera versión.</p>
          </section>
        }
      }

      @if (tab() === 'import') {
        <section class="panel import-panel">
          <div>
            <span class="eyebrow">CSV / Excel</span>
            <h2>Analizar lista</h2>
            <p>El archivo se valida antes de agregar cualquier registro.</p>
          </div>
          <div class="import-template-picker">
            <label>
              <span>Formato</span>
              <select [value]="templateFormat()" (change)="setTemplateFormat(eventValue($event))">
                @for (option of formatOptions; track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
            </label>
            <label>
              <span>Idioma</span>
              <select
                [value]="templateLanguage()"
                (change)="setTemplateLanguage(eventValue($event))"
              >
                @for (option of languageOptions; track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
            </label>
          </div>
          <div class="button-row">
            <button class="btn btn--secondary" type="button" (click)="downloadTemplate()">
              Descargar plantilla
            </button>
            <label class="file-button btn btn--primary">
              Seleccionar archivo
              <input
                type="file"
                accept=".csv,text/csv,.xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                (change)="analyze($event)"
              />
            </label>
          </div>
          <details class="disclosure">
            <summary>Ver guía de llenado</summary>
            <div class="field-guide-scroll">
              <table class="field-guide">
                <thead>
                  <tr>
                    <th>Campo</th>
                    <th>Obligatorio</th>
                    <th>Descripción</th>
                    <th>Valores válidos</th>
                  </tr>
                </thead>
                <tbody>
                  @for (field of fieldGuide(); track field.label) {
                    <tr>
                      <td>{{ field.label }}</td>
                      <td>{{ field.required ? 'Sí' : 'No' }}</td>
                      <td>{{ field.description }}</td>
                      <td>{{ field.validValues }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </details>
          @if (analysis(); as result) {
            <div class="import-summary">
              <span
                ><strong>{{ result.totalRows }}</strong
                ><small>filas</small></span
              >
              <span
                ><strong>{{ result.validRows }}</strong
                ><small>válidas</small></span
              >
              <span
                ><strong>{{ result.errorRows }}</strong
                ><small>con error</small></span
              >
            </div>
            @for (row of result.preview; track row.rowNumber) {
              <div class="preview-row" [class.preview-row--error]="!row.isValid">
                <strong>#{{ row.rowNumber }}</strong>
                <span>{{ row.groupName }}</span>
                <span>{{ row.guestName }}</span>
                <span>{{ row.errors.join(' · ') || 'Lista' }}</span>
              </div>
            }
            <button
              class="btn btn--primary"
              type="button"
              [disabled]="result.errorRows > 0"
              (click)="confirmImport(result.importId)"
            >
              Confirmar importación
            </button>
            <p>La confirmación requiere que todas las filas sean válidas y es idempotente.</p>
          }
          @if (importResult(); as result) {
            <div class="notice notice--success">
              <strong>Importación terminada</strong>
              <span
                >{{ result.createdGroups }} grupos y {{ result.createdGuests }} invitados
                creados.</span
              >
            </div>
          }
        </section>
      }

      @if (tab() === 'links') {
        <section class="panel stack">
          <div>
            <span class="eyebrow">Accesos privados</span>
            <h2>Enlaces compartidos</h2>
            <p>Los accesos activos se pueden copiar sin exponer el token en la base de datos.</p>
          </div>
          @for (link of links(); track link.id) {
            <div class="group-card">
              <div class="group-card__head">
                <div>
                  <span class="status-chip" [attr.data-status]="link.status">{{
                    link.status
                  }}</span>
                  <h3>{{ groupName(link.invitationGroupId) }}</h3>
                </div>
                <span>{{ link.openCount }} aperturas</span>
              </div>
              <div class="button-row">
                <button
                  class="btn btn--secondary"
                  type="button"
                  [disabled]="!link.publicUrl"
                  (click)="copyLink(link)"
                >
                  Copiar enlace
                </button>
                <button
                  class="btn btn--secondary"
                  type="button"
                  [disabled]="!link.publicUrl"
                  (click)="openWhatsApp(link)"
                >
                  Abrir WhatsApp
                </button>
                <button
                  class="btn btn--ghost"
                  type="button"
                  [disabled]="link.status !== 'Active'"
                  (click)="markShared(link)"
                >
                  Marcar compartido
                </button>
              </div>
            </div>
          } @empty {
            <div class="empty-state">
              <h3>Sin enlaces disponibles</h3>
              <p>La planner debe publicar el diseño y generar los accesos.</p>
            </div>
          }
        </section>
      }
    </div>
  `,
})
export class PortalGuestExperiencePage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly eventId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly workspace = signal<PortalGuestWorkspace | null>(null);
  protected readonly analysis = signal<GuestImportAnalysis | null>(null);
  protected readonly importResult = signal<GuestImportResult | null>(null);
  protected readonly links = signal<GuestAccessLink[]>([]);
  protected readonly duplicates = signal<GuestDuplicateSuggestion[]>([]);
  protected readonly templateFormat = signal<GuestImportTemplateFormat>('csv');
  protected readonly templateLanguage = signal<GuestImportTemplateLanguage>('es');
  protected readonly formatOptions = GUEST_IMPORT_FORMAT_OPTIONS;
  protected readonly languageOptions = GUEST_IMPORT_LANGUAGE_OPTIONS;
  protected readonly tab = signal<'list' | 'design' | 'import' | 'links'>('list');
  protected readonly editingGroupId = signal<string | null>(null);
  protected readonly editingGuestId = signal<string | null>(null);
  protected readonly groupTypes: InvitationGroupType[] = [
    'Individual',
    'Couple',
    'Family',
    'Group',
    'Company',
    'CorporateTable',
    'Other',
  ];
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

  protected readonly groupForm = new FormGroup({
    displayName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    groupType: new FormControl<InvitationGroupType>('Family', { nonNullable: true }),
    allowedGuestCount: new FormControl(1, { nonNullable: true, validators: [Validators.min(1)] }),
    allowUnnamedCompanions: new FormControl(false, { nonNullable: true }),
    maxUnnamedCompanions: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)],
    }),
  });
  protected readonly guestForm = new FormGroup({
    invitationGroupId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    lastName: new FormControl('', { nonNullable: true }),
    guestType: new FormControl<GuestType>('Other', { nonNullable: true }),
    ageCategory: new FormControl<AgeCategory>('Adult', { nonNullable: true }),
    isPrimaryContact: new FormControl(false, { nonNullable: true }),
    isVip: new FormControl(false, { nonNullable: true }),
  });
  protected readonly reviewForm = new FormGroup({
    message: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
  });

  constructor() {
    this.load();
  }

  protected guestsFor(groupId: string): PortalGuest[] {
    return (this.workspace()?.guests ?? []).filter((guest) => guest.invitationGroupId === groupId);
  }

  protected saveGroup(): void {
    if (this.groupForm.invalid) return;
    const value = this.groupForm.getRawValue();
    const request = {
      ...value,
      maxUnnamedCompanions: value.allowUnnamedCompanions ? value.maxUnnamedCompanions : 0,
    };
    const groupId = this.editingGroupId();
    const operation = groupId
      ? this.api.updatePortalInvitationGroup(this.eventId, groupId, request)
      : this.api.createPortalInvitationGroup(this.eventId, request);
    operation.subscribe({
      next: () => {
        this.cancelGroupEdit();
        this.toast.success(groupId ? 'Grupo actualizado.' : 'Grupo creado.');
        this.load();
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected editGroup(group: PortalInvitationGroup): void {
    this.editingGroupId.set(group.id);
    this.groupForm.reset({
      displayName: group.displayName,
      groupType: group.groupType,
      allowedGuestCount: group.allowedGuestCount,
      allowUnnamedCompanions: group.allowUnnamedCompanions,
      maxUnnamedCompanions: group.maxUnnamedCompanions,
    });
  }

  protected cancelGroupEdit(): void {
    this.editingGroupId.set(null);
    this.groupForm.reset({
      displayName: '',
      groupType: 'Family',
      allowedGuestCount: 1,
      allowUnnamedCompanions: false,
      maxUnnamedCompanions: 0,
    });
  }

  protected archiveGroup(group: PortalInvitationGroup): void {
    if (!window.confirm(`¿Archivar el grupo “${group.displayName}”?`)) return;
    this.api.archivePortalInvitationGroup(this.eventId, group.id).subscribe({
      next: () => {
        this.toast.success('Grupo archivado.');
        this.load();
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected saveGuest(): void {
    if (this.guestForm.invalid) return;
    const value = this.guestForm.getRawValue();
    const request = {
      invitationGroupId: value.invitationGroupId,
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      guestType: value.guestType,
      ageCategory: value.ageCategory,
      isPrimaryContact: value.isPrimaryContact,
      isVip: value.isVip,
      sortOrder: 0,
    };
    const guestId = this.editingGuestId();
    const operation = guestId
      ? this.api.updatePortalGuest(this.eventId, guestId, request)
      : this.api.createPortalGuest(this.eventId, request);
    operation.subscribe({
      next: () => {
        this.cancelGuestEdit();
        this.toast.success(guestId ? 'Invitado actualizado.' : 'Invitado agregado.');
        this.load();
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected editGuest(guest: PortalGuest): void {
    this.editingGuestId.set(guest.id);
    this.guestForm.reset({
      invitationGroupId: guest.invitationGroupId ?? '',
      firstName: guest.firstName,
      lastName: guest.lastName,
      guestType: guest.guestType,
      ageCategory: guest.ageCategory,
      isPrimaryContact: guest.isPrimaryContact,
      isVip: guest.isVip,
    });
  }

  protected cancelGuestEdit(): void {
    this.editingGuestId.set(null);
    this.guestForm.reset({
      invitationGroupId: '',
      firstName: '',
      lastName: '',
      guestType: 'Other',
      ageCategory: 'Adult',
      isPrimaryContact: false,
      isVip: false,
    });
  }

  protected archiveGuest(guest: PortalGuest): void {
    const displayName = `${guest.firstName} ${guest.lastName}`.trim();
    if (!window.confirm(`¿Archivar a “${displayName}”?`)) return;
    this.api.archivePortalGuest(this.eventId, guest.id).subscribe({
      next: () => {
        this.toast.success('Invitado archivado.');
        this.load();
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected review(versionId: string, action: 'comments' | 'approve' | 'request-changes'): void {
    const design = this.workspace()?.design;
    if (!design) return;
    const message = this.reviewForm.controls.message.value.trim();
    if (action === 'request-changes' && !message) {
      this.toast.error('Describe los cambios solicitados.');
      return;
    }
    this.api.reviewPortalInvitation(this.eventId, design.id, versionId, action, message).subscribe({
      next: (updated) => {
        this.workspace.update((workspace) =>
          workspace ? { ...workspace, design: updated } : workspace,
        );
        this.reviewForm.reset({ message: '' });
        this.toast.success(action === 'approve' ? 'Versión aprobada.' : 'Revisión registrada.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected analyze(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.importResult.set(null);
    this.api.analyzePortalGuestImport(this.eventId, file).subscribe({
      next: (analysis) => this.analysis.set(analysis),
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected confirmImport(importId: string): void {
    this.api.confirmPortalGuestImport(this.eventId, importId).subscribe({
      next: (result) => {
        this.importResult.set(result);
        this.toast.success('Lista importada.');
        this.load();
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected setTemplateFormat(value: string): void {
    this.templateFormat.set(value === 'xlsx' ? 'xlsx' : 'csv');
  }

  protected setTemplateLanguage(value: string): void {
    this.templateLanguage.set(value === 'en' ? 'en' : 'es');
  }

  protected fieldGuide(): GuestImportFieldGuideEntry[] {
    return guestImportFieldGuide(this.templateLanguage());
  }

  protected eventValue(event: Event): string {
    const target = event.target;
    return target instanceof HTMLInputElement
      || target instanceof HTMLSelectElement
      || target instanceof HTMLTextAreaElement
      ? target.value
      : '';
  }

  protected downloadTemplate(): void {
    const format = this.templateFormat();
    const language = this.templateLanguage();
    this.api.downloadPortalGuestImportTemplate(this.eventId, format, language).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = templateFileName(format, language);
        anchor.click();
        URL.revokeObjectURL(url);
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected openLinks(): void {
    this.tab.set('links');
    this.api.getPortalGuestLinks(this.eventId).subscribe({
      next: (links) => this.links.set(links),
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected copyLink(link: GuestAccessLink): void {
    if (!link.publicUrl) return;
    void navigator.clipboard.writeText(link.publicUrl).then(
      () => this.toast.success('Enlace copiado.'),
      () => this.toast.error('No fue posible copiar el enlace.'),
    );
  }

  protected openWhatsApp(link: GuestAccessLink): void {
    if (!link.publicUrl) return;
    const message = `Hola, ${this.groupName(link.invitationGroupId)}. Te compartimos la invitación: ${link.publicUrl}`;
    window.open(
      `https://wa.me/?text=${encodeURIComponent(message)}`,
      '_blank',
      'noopener,noreferrer',
    );
  }

  protected markShared(link: GuestAccessLink): void {
    this.api.markPortalGuestLinkShared(this.eventId, link.id).subscribe({
      next: (updated) => {
        this.links.update((links) =>
          links.map((item) => (item.id === updated.id ? updated : item)),
        );
        this.toast.success('Enlace marcado como compartido.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected groupName(groupId: string): string {
    return (
      this.workspace()?.groups.find((group) => group.id === groupId)?.displayName ??
      'Grupo de invitación'
    );
  }

  protected blockText(
    block: { content: Record<string, string | number | boolean | null> },
    field: string,
  ): string {
    const value = block.content[field];
    return typeof value === 'string' ? value : '';
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

  private load(): void {
    this.api
      .getPortalGuestWorkspace(this.eventId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (workspace) => this.workspace.set(workspace),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
    this.api
      .getPortalGuestDuplicates(this.eventId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (duplicates) => this.duplicates.set(duplicates),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }
}

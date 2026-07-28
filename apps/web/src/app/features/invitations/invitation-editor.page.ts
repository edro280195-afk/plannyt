import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { debounceTime, finalize, forkJoin } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  GuestAccessLink,
  GuestDashboard,
  GuestExperience,
  InvitationBlock,
  InvitationBlockType,
  InvitationDesign,
  InvitationTemplate,
  InvitationTheme,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-invitation-editor-page',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page invitation-studio">
      <a class="back-link" [routerLink]="['/app/events', eventId, 'guests']"
        >← Volver a invitados</a
      >
      <header class="page-header">
        <div>
          <span class="eyebrow">Estudio de invitación</span>
          <h1>Diseño y publicación</h1>
          <p>Plantillas controladas, revisión por versión y vista previa fiel.</p>
        </div>
        <div class="page-header__actions">
          @if (activeDesign(); as design) {
            <span class="save-state" [attr.data-state]="saveState()">{{ saveStateLabel() }}</span>
            @if (experience()?.status === 'Published') {
              <button class="btn btn--quiet" type="button" (click)="toggleExperience(false)">
                Suspender experiencia
              </button>
            } @else if (experience()?.status === 'Suspended') {
              <button class="btn btn--secondary" type="button" (click)="toggleExperience(true)">
                Reanudar experiencia
              </button>
            }
            @if (
              organization.hasPermission('invitation-designs.submit-review') &&
              design.status !== 'InReview'
            ) {
              <button class="btn btn--secondary" type="button" (click)="submitReview()">
                Enviar a revisión
              </button>
            }
            @if (organization.hasPermission('invitation-designs.publish')) {
              <button
                class="btn btn--primary"
                type="button"
                [disabled]="design.status !== 'Approved'"
                (click)="publish()"
              >
                Publicar versión aprobada
              </button>
            }
          }
        </div>
      </header>

      @if (experience()) {
        <details class="panel experience-settings">
          <summary>
            <span
              ><span class="eyebrow">Experiencia pública</span
              ><strong>Textos y visibilidad</strong></span
            >
            <small>Configura qué información del evento podrá ver cada grupo.</small>
          </summary>
          <form [formGroup]="experienceForm" (ngSubmit)="saveExperience()" class="form-stack">
            <div class="form-grid">
              <label>Título público <input formControlName="publicTitle" /></label>
              <label>Celebrantes <input formControlName="celebrantDisplayName" /></label>
            </div>
            <div class="form-grid">
              <label
                >Mensaje de bienvenida
                <textarea rows="3" formControlName="welcomeMessage"></textarea>
              </label>
              <label
                >Mensaje de cierre <textarea rows="3" formControlName="closingMessage"></textarea>
              </label>
            </div>
            <div class="form-grid">
              <label
                >Idioma
                <select formControlName="language">
                  <option value="es">Español</option>
                  <option value="en">English</option>
                </select>
              </label>
              <label class="check-line"
                ><input type="checkbox" formControlName="privateAccessOnly" /><span
                  >Requerir enlace privado</span
                ></label
              >
            </div>
            <div class="button-row">
              <label class="check-line"
                ><input type="checkbox" formControlName="showEventName" /><span
                  >Nombre del evento</span
                ></label
              >
              <label class="check-line"
                ><input type="checkbox" formControlName="showEventDate" /><span
                  >Fecha y cuenta regresiva</span
                ></label
              >
              <label class="check-line"
                ><input type="checkbox" formControlName="showParticipantNames" /><span
                  >Integrantes del grupo</span
                ></label
              >
              <label class="check-line"
                ><input type="checkbox" formControlName="showCity" /><span>Ciudad</span></label
              >
            </div>
            <button class="btn btn--secondary" type="submit" [disabled]="experienceForm.invalid">
              Guardar configuración
            </button>
          </form>
        </details>
      }

      @if (!activeDesign()) {
        <section class="template-picker">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Punto de partida</span>
              <h2>Elige una plantilla</h2>
            </div>
          </div>
          <div class="template-grid">
            @for (template of templates(); track template.id) {
              <button class="template-card" type="button" (click)="createFromTemplate(template)">
                <span
                  class="template-swatch"
                  [style.background]="template.theme.backgroundColor"
                  [style.color]="template.theme.textColor"
                >
                  <i [style.background]="template.theme.accentColor"></i>
                  Aa
                </span>
                <strong>{{ template.name }}</strong>
                <small>{{ template.description }}</small>
                @if (template.isGlobal) {
                  <span class="tag-chip">Plannyt</span>
                }
              </button>
            }
          </div>
        </section>
      } @else if (activeDesign(); as design) {
        <section class="studio-status-bar">
          <span class="status-chip" [attr.data-status]="design.status">{{
            statusLabel(design.status)
          }}</span>
          <span>Versión siguiente {{ design.nextVersionNumber }}</span>
          <span>{{ design.blocks.length }} bloques</span>
          <span>{{ design.accessibilityWarnings.length }} alertas de accesibilidad</span>
          <select [value]="design.id" (change)="selectDesign($event)">
            @for (item of designs(); track item.id) {
              <option [value]="item.id">{{ item.name }}</option>
            }
          </select>
        </section>

        @if (design.accessibilityWarnings.length > 0) {
          <div class="notice notice--warning">
            <span>{{ design.accessibilityWarnings.join(' ') }}</span>
          </div>
        }

        <section class="studio-layout">
          <aside class="studio-panel studio-panel--blocks">
            <div class="studio-panel__heading">
              <div>
                <span class="eyebrow">Estructura</span>
                <h2>Bloques</h2>
              </div>
              <select [formControl]="newBlockType">
                @for (type of blockTypes; track type) {
                  <option [value]="type">{{ blockLabel(type) }}</option>
                }
              </select>
              <button
                class="icon-button"
                type="button"
                aria-label="Agregar bloque"
                (click)="addBlock()"
              >
                +
              </button>
            </div>
            <div class="block-list">
              @for (block of design.blocks; track block.id; let index = $index) {
                <div
                  class="block-row"
                  [class.is-active]="selectedBlockId() === block.id"
                  role="button"
                  tabindex="0"
                  (click)="selectedBlockId.set(block.id)"
                  (keydown.enter)="selectedBlockId.set(block.id)"
                >
                  <span class="drag-handle">⋮⋮</span>
                  <span
                    ><strong>{{ blockLabel(block.type) }}</strong
                    ><small>{{ block.visible ? visibilityLabel(block) : 'Oculto' }}</small></span
                  >
                  <span class="block-actions">
                    <button
                      class="icon-button"
                      type="button"
                      aria-label="Subir"
                      [disabled]="index === 0"
                      (click)="moveBlock(block.id, -1, $event)"
                    >
                      ↑
                    </button>
                    <button
                      class="icon-button"
                      type="button"
                      aria-label="Bajar"
                      [disabled]="index === design.blocks.length - 1"
                      (click)="moveBlock(block.id, 1, $event)"
                    >
                      ↓
                    </button>
                  </span>
                </div>
              }
            </div>
            @if (selectedBlock(); as block) {
              <div class="block-settings">
                <h3>{{ blockLabel(block.type) }}</h3>
                <label class="check-line">
                  <input
                    type="checkbox"
                    [checked]="block.visible"
                    (change)="toggleBlock(block.id)"
                  />
                  <span>Mostrar bloque</span>
                </label>
                <label
                  >Visibilidad
                  <select [value]="block.visibility" (change)="changeVisibility(block.id, $event)">
                    <option value="Everyone">Todas las invitaciones</option>
                    <option value="InvitationGroup">Grupo específico</option>
                    <option value="HasTag">Con etiqueta</option>
                    <option value="GuestType">Tipo de invitado</option>
                    <option value="VipOnly">Solo VIP</option>
                  </select>
                </label>
                @for (field of editableFields(block); track field) {
                  <label
                    >{{ fieldLabel(field) }}
                    @if (field === 'body' || field === 'details' || field === 'text') {
                      <textarea
                        rows="4"
                        [value]="blockText(block, field)"
                        (input)="updateBlockText(block.id, field, $event)"
                      ></textarea>
                    } @else {
                      <input
                        [value]="blockText(block, field)"
                        (input)="updateBlockText(block.id, field, $event)"
                      />
                    }
                  </label>
                }
                <button
                  class="btn btn--quiet danger-text"
                  type="button"
                  (click)="removeBlock(block.id)"
                >
                  Eliminar bloque
                </button>
              </div>
            }
          </aside>

          <main class="preview-stage">
            <div class="preview-toolbar">
              <span>Vista previa</span>
              <div class="device-toggle">
                <button
                  type="button"
                  [class.is-active]="previewDevice() === 'mobile'"
                  (click)="previewDevice.set('mobile')"
                >
                  Móvil
                </button>
                <button
                  type="button"
                  [class.is-active]="previewDevice() === 'desktop'"
                  (click)="previewDevice.set('desktop')"
                >
                  Escritorio
                </button>
              </div>
            </div>
            <div
              class="invitation-device"
              [class.invitation-device--desktop]="previewDevice() === 'desktop'"
            >
              <div
                class="invitation-canvas"
                [style.background]="design.theme.backgroundColor"
                [style.color]="design.theme.textColor"
                [style.--invite-accent]="design.theme.accentColor"
                [style.--invite-surface]="design.theme.surfaceColor"
              >
                @for (block of design.blocks; track block.id) {
                  @if (block.visible) {
                    <section class="invite-block" [attr.data-block]="block.type">
                      @switch (block.type) {
                        @case ('Cover') {
                          <small>{{ blockText(block, 'eyebrow') }}</small>
                          <h1>{{ blockText(block, 'title') }}</h1>
                          <p>{{ previewValue(blockText(block, 'subtitle')) }}</p>
                        }
                        @case ('Participants') {
                          <h2>{{ blockText(block, 'heading') }}</h2>
                          <div class="participant-pills">
                            @for (guest of previewGuests(); track guest.id) {
                              <span>{{ guest.firstName }} {{ guest.lastName }}</span>
                            } @empty {
                              <span>Participantes del grupo</span>
                            }
                          </div>
                        }
                        @case ('EventDate') {
                          <h2>{{ blockText(block, 'heading') }}</h2>
                          <p class="invite-date">Sábado · 6:00 p. m.</p>
                        }
                        @case ('Divider') {
                          <hr />
                        }
                        @case ('CustomButton') {
                          <button class="invite-button" type="button" disabled>
                            {{ blockText(block, 'label') }}
                          </button>
                        }
                        @default {
                          <h2>{{ blockText(block, 'title') || blockText(block, 'heading') }}</h2>
                          <p>
                            {{
                              blockText(block, 'body') ||
                                blockText(block, 'details') ||
                                blockText(block, 'text') ||
                                blockText(block, 'value')
                            }}
                          </p>
                        }
                      }
                    </section>
                  }
                }
                <section class="invite-block invite-block--demo">
                  <button type="button" disabled>Confirmar asistencia</button>
                  <small>Demostración · RSVP estará disponible en una fase posterior</small>
                </section>
              </div>
            </div>
          </main>

          <aside class="studio-panel studio-panel--theme">
            <div>
              <span class="eyebrow">Identidad visual</span>
              <h2>Tema</h2>
            </div>
            <form [formGroup]="themeForm" class="form-stack">
              <div class="color-grid">
                <label>Fondo <input type="color" formControlName="backgroundColor" /></label>
                <label>Superficie <input type="color" formControlName="surfaceColor" /></label>
                <label>Texto <input type="color" formControlName="textColor" /></label>
                <label>Acento <input type="color" formControlName="accentColor" /></label>
              </div>
              <label
                >Tipografía de títulos
                <select formControlName="headingFont">
                  @for (font of fonts; track font) {
                    <option [value]="font">{{ font }}</option>
                  }
                </select>
              </label>
              <label
                >Tipografía de texto
                <select formControlName="bodyFont">
                  @for (font of fonts; track font) {
                    <option [value]="font">{{ font }}</option>
                  }
                </select>
              </label>
              <label
                >Movimiento
                <select formControlName="animation">
                  <option value="None">Sin animación</option>
                  <option value="Reduced">Reducida</option>
                  <option value="Standard">Estándar</option>
                </select>
              </label>
            </form>

            <details open>
              <summary>Historial y aprobación</summary>
              @for (version of design.versions; track version.id) {
                <div class="version-row">
                  <span>v{{ version.versionNumber }}</span>
                  <span
                    ><strong>{{ version.approvedAt ? 'Aprobada' : 'En revisión' }}</strong
                    ><small>{{ version.createdAt }}</small></span
                  >
                </div>
              } @empty {
                <p class="muted">Guarda y envía el borrador a revisión.</p>
              }
              @if (design.versions[0]; as version) {
                <form [formGroup]="commentForm" class="form-stack" (ngSubmit)="comment(version.id)">
                  <label>Comentario <textarea rows="3" formControlName="message"></textarea></label>
                  <button class="btn btn--secondary" type="submit">Comentar versión</button>
                </form>
                @if (
                  organization.hasPermission('invitation-designs.approve') &&
                  design.status === 'InReview'
                ) {
                  <div class="button-row">
                    <button class="btn btn--primary" type="button" (click)="approve(version.id)">
                      Aprobar
                    </button>
                    <button
                      class="btn btn--secondary"
                      type="button"
                      (click)="requestChanges(version.id)"
                    >
                      Solicitar cambios
                    </button>
                  </div>
                }
              }
            </details>
          </aside>
        </section>

        <section class="panel stack link-panel">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Distribución privada</span>
              <h2>Enlaces por grupo</h2>
            </div>
            <span class="security-note"
              >Los enlaces no confirman entrega y solo se muestran completos al generarlos.</span
            >
          </div>
          @for (group of dashboard()?.groups ?? []; track group.id) {
            <div class="link-row">
              <span
                ><strong>{{ group.displayName }}</strong
                ><small
                  >{{ group.namedGuestCount }}/{{ group.allowedGuestCount }} personas</small
                ></span
              >
              @if (linkFor(group.id); as link) {
                <span
                  ><strong>{{ link.status }}</strong
                  ><small>{{ link.openCount }} aperturas</small></span
                >
                @if (generatedUrls()[link.id]; as url) {
                  <div class="copy-box">
                    <input readonly [value]="url" />
                    <button class="btn btn--secondary" type="button" (click)="copy(url)">
                      Copiar
                    </button>
                    <a
                      class="btn btn--quiet"
                      target="_blank"
                      rel="noopener noreferrer"
                      [href]="whatsAppUrl(group.displayName, url)"
                      >Texto para WhatsApp</a
                    >
                  </div>
                } @else {
                  <small>Por seguridad, regenera el enlace si necesitas copiarlo de nuevo.</small>
                }
                <span class="row-actions">
                  <button class="btn btn--quiet" type="button" (click)="markShared(link)">
                    Marcar compartido
                  </button>
                  @if (organization.hasPermission('guest-links.regenerate')) {
                    <button class="btn btn--quiet" type="button" (click)="regenerate(link)">
                      Regenerar
                    </button>
                  }
                  @if (organization.hasPermission('guest-links.revoke')) {
                    <button class="btn btn--quiet danger-text" type="button" (click)="revoke(link)">
                      Revocar
                    </button>
                  }
                </span>
              } @else {
                <button
                  class="btn btn--secondary"
                  type="button"
                  [disabled]="experience()?.status !== 'Published'"
                  (click)="generate(group.id)"
                >
                  Generar enlace
                </button>
              }
            </div>
          }
        </section>
      }
    </div>
  `,
})
export class InvitationEditorPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly eventId = this.route.snapshot.paramMap.get('id') ?? '';
  private readonly organizationId = this.organization.requireOrganizationId();
  protected readonly templates = signal<InvitationTemplate[]>([]);
  protected readonly designs = signal<InvitationDesign[]>([]);
  protected readonly activeDesign = signal<InvitationDesign | null>(null);
  protected readonly experience = signal<GuestExperience | null>(null);
  protected readonly dashboard = signal<GuestDashboard | null>(null);
  protected readonly links = signal<GuestAccessLink[]>([]);
  protected readonly generatedUrls = signal<Record<string, string>>({});
  protected readonly selectedBlockId = signal<string | null>(null);
  protected readonly previewDevice = signal<'mobile' | 'desktop'>('mobile');
  protected readonly saveState = signal<'saved' | 'pending' | 'saving' | 'error'>('saved');
  protected readonly newBlockType = new FormControl<InvitationBlockType>('Text', {
    nonNullable: true,
  });
  protected readonly blockTypes: InvitationBlockType[] = [
    'Cover',
    'Greeting',
    'Participants',
    'EventDate',
    'Countdown',
    'Story',
    'Image',
    'GalleryPreview',
    'Text',
    'Divider',
    'DressCode',
    'Contact',
    'CustomButton',
    'Footer',
  ];
  protected readonly fonts = ['inter', 'source-serif', 'playfair', 'montserrat', 'nunito', 'lora'];
  protected readonly themeForm = new FormGroup({
    backgroundColor: new FormControl('#FAF7F2', { nonNullable: true }),
    surfaceColor: new FormControl('#FFFFFF', { nonNullable: true }),
    textColor: new FormControl('#292421', { nonNullable: true }),
    accentColor: new FormControl('#A85D43', { nonNullable: true }),
    headingFont: new FormControl('playfair', { nonNullable: true }),
    bodyFont: new FormControl('inter', { nonNullable: true }),
    animation: new FormControl<'None' | 'Reduced' | 'Standard'>('Reduced', { nonNullable: true }),
  });
  protected readonly experienceForm = new FormGroup({
    language: new FormControl('es', { nonNullable: true }),
    publicTitle: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(160)],
    }),
    celebrantDisplayName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    welcomeMessage: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(1000)],
    }),
    closingMessage: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(1000)],
    }),
    showEventName: new FormControl(true, { nonNullable: true }),
    showEventDate: new FormControl(true, { nonNullable: true }),
    showParticipantNames: new FormControl(true, { nonNullable: true }),
    showCity: new FormControl(true, { nonNullable: true }),
    privateAccessOnly: new FormControl(true, { nonNullable: true }),
  });
  protected readonly commentForm = new FormGroup({
    message: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
  });
  private hydrating = false;
  private autosaveHandle: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.load();
    this.themeForm.valueChanges
      .pipe(debounceTime(500), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.hydrating) return;
        const design = this.activeDesign();
        if (!design) return;
        this.activeDesign.set({ ...design, theme: this.buildTheme(design.theme) });
        this.queueAutosave();
      });
  }

  protected selectedBlock(): InvitationBlock | null {
    return this.activeDesign()?.blocks.find((block) => block.id === this.selectedBlockId()) ?? null;
  }

  protected previewGuests() {
    const firstGroup = this.dashboard()?.groups[0];
    return (this.dashboard()?.guests ?? []).filter(
      (guest) => guest.invitationGroupId === firstGroup?.id,
    );
  }

  protected createFromTemplate(template: InvitationTemplate): void {
    this.api
      .createInvitationDesign(this.organizationId, this.eventId, {
        name: `${template.name} · Invitación`,
        templateId: template.id,
      })
      .subscribe({
        next: (design) => {
          this.designs.update((items) => [design, ...items]);
          this.hydrateDesign(design);
          this.toast.success('Diseño creado desde la plantilla.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected selectDesign(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    const design = this.designs().find((item) => item.id === id);
    if (design) this.hydrateDesign(design);
  }

  protected addBlock(): void {
    const design = this.activeDesign();
    if (!design) return;
    const type = this.newBlockType.value;
    const block: InvitationBlock = {
      id: crypto.randomUUID(),
      type,
      visible: true,
      visibility: 'Everyone',
      visibilityValue: null,
      sortOrder: design.blocks.length,
      content: this.defaultContent(type),
      presentation: {
        backgroundToken: 'default',
        textAlign: 'center',
        emphasis: 'normal',
        width: 'content',
      },
    };
    this.activeDesign.set({ ...design, blocks: [...design.blocks, block] });
    this.selectedBlockId.set(block.id);
    this.queueAutosave();
  }

  protected moveBlock(id: string, direction: -1 | 1, event: Event): void {
    event.stopPropagation();
    const design = this.activeDesign();
    if (!design) return;
    const blocks = [...design.blocks];
    const index = blocks.findIndex((block) => block.id === id);
    const target = index + direction;
    if (index < 0 || target < 0 || target >= blocks.length) return;
    const current = blocks[index]!;
    blocks[index] = blocks[target]!;
    blocks[target] = current;
    this.activeDesign.set({
      ...design,
      blocks: blocks.map((block, sortOrder) => ({ ...block, sortOrder })),
    });
    this.queueAutosave();
  }

  protected removeBlock(id: string): void {
    const design = this.activeDesign();
    if (!design || design.blocks.length <= 1) {
      this.toast.error('El diseño requiere al menos un bloque.');
      return;
    }
    this.activeDesign.set({
      ...design,
      blocks: design.blocks
        .filter((block) => block.id !== id)
        .map((block, sortOrder) => ({ ...block, sortOrder })),
    });
    this.selectedBlockId.set(null);
    this.queueAutosave();
  }

  protected toggleBlock(id: string): void {
    this.updateBlock(id, (block) => ({ ...block, visible: !block.visible }));
  }

  protected changeVisibility(id: string, event: Event): void {
    const visibility = (event.target as HTMLSelectElement).value as InvitationBlock['visibility'];
    this.updateBlock(id, (block) => ({
      ...block,
      visibility,
      visibilityValue:
        visibility === 'Everyone' || visibility === 'VipOnly' ? null : block.visibilityValue,
    }));
  }

  protected updateBlockText(id: string, field: string, event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLTextAreaElement).value;
    this.updateBlock(id, (block) => ({ ...block, content: { ...block.content, [field]: value } }));
  }

  protected editableFields(block: InvitationBlock): string[] {
    return Object.keys(block.content).filter(
      (field) => typeof block.content[field] === 'string' && !['url', 'imageUrl'].includes(field),
    );
  }

  protected blockText(block: InvitationBlock, field: string): string {
    const value = block.content[field];
    return typeof value === 'string' ? value : '';
  }

  protected submitReview(): void {
    const design = this.activeDesign();
    if (!design) return;
    this.saveNow(() => {
      this.api.submitInvitationReview(this.organizationId, this.eventId, design.id).subscribe({
        next: (updated) => {
          this.replaceDesign(updated);
          this.toast.success('Versión enviada a revisión.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
    });
  }

  protected comment(versionId: string): void {
    const design = this.activeDesign();
    if (!design || this.commentForm.invalid) return;
    this.review(versionId, 'comments', this.commentForm.controls.message.value);
  }

  protected approve(versionId: string): void {
    this.review(versionId, 'approve', this.commentForm.controls.message.value);
  }

  protected requestChanges(versionId: string): void {
    const message = this.commentForm.controls.message.value.trim();
    if (!message) {
      this.toast.error('Escribe el cambio solicitado.');
      return;
    }
    this.review(versionId, 'request-changes', message);
  }

  protected publish(): void {
    const design = this.activeDesign();
    if (!design) return;
    this.api.publishInvitationDesign(this.organizationId, this.eventId, design.id).subscribe({
      next: (updated) => {
        this.replaceDesign(updated);
        this.experience.update((value) =>
          value
            ? {
                ...value,
                status: 'Published',
                activeInvitationDesignId: updated.id,
                activeVersionId: updated.approvedVersionId,
              }
            : value,
        );
        this.toast.success('Invitación publicada.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected toggleExperience(resume: boolean): void {
    const operation = resume
      ? this.api.resumeGuestExperience(this.organizationId, this.eventId)
      : this.api.suspendGuestExperience(this.organizationId, this.eventId);
    operation.subscribe({
      next: (experience) => {
        this.experience.set(experience);
        this.toast.success(resume ? 'Experiencia reanudada.' : 'Experiencia suspendida.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected saveExperience(): void {
    if (this.experienceForm.invalid) return;
    const value = this.experienceForm.getRawValue();
    this.api
      .updateGuestExperience(this.organizationId, this.eventId, {
        ...value,
        welcomeMessage: value.welcomeMessage.trim() || null,
        closingMessage: value.closingMessage.trim() || null,
      })
      .subscribe({
        next: (experience) => {
          this.experience.set(experience);
          this.hydrateExperience(experience);
          this.toast.success('Configuración pública guardada.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected generate(groupId: string): void {
    this.api.generateGuestLink(this.organizationId, this.eventId, groupId, null).subscribe({
      next: (link) => {
        this.links.update((links) => [
          link,
          ...links.filter((item) => item.invitationGroupId !== groupId || item.status !== 'Active'),
        ]);
        if (link.publicUrl)
          this.generatedUrls.update((urls) => ({ ...urls, [link.id]: link.publicUrl! }));
        this.toast.success('Enlace privado generado.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected regenerate(link: GuestAccessLink): void {
    if (!confirm('El enlace anterior dejará de funcionar. ¿Continuar?')) return;
    this.api.regenerateGuestLink(this.organizationId, this.eventId, link.id, null).subscribe({
      next: (created) => {
        this.links.update((links) => [
          created,
          ...links.map((item) =>
            item.id === link.id ? { ...item, status: 'Replaced' as const } : item,
          ),
        ]);
        if (created.publicUrl)
          this.generatedUrls.update((urls) => ({ ...urls, [created.id]: created.publicUrl! }));
        this.toast.success('Enlace regenerado; el anterior fue reemplazado.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected revoke(link: GuestAccessLink): void {
    if (!confirm('¿Revocar este enlace privado?')) return;
    this.api.revokeGuestLink(this.organizationId, this.eventId, link.id).subscribe({
      next: () => {
        this.links.update((links) =>
          links.map((item) => (item.id === link.id ? { ...item, status: 'Revoked' } : item)),
        );
        this.toast.success('Enlace revocado.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected markShared(link: GuestAccessLink): void {
    this.api.markGuestLinkShared(this.organizationId, this.eventId, link.id).subscribe({
      next: (updated) => {
        this.links.update((links) => links.map((item) => (item.id === link.id ? updated : item)));
        this.toast.success('Marcado como compartido manualmente.');
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  protected linkFor(groupId: string): GuestAccessLink | null {
    return (
      this.links().find((link) => link.invitationGroupId === groupId && link.status === 'Active') ??
      null
    );
  }

  protected copy(value: string): void {
    navigator.clipboard
      .writeText(value)
      .then(() => this.toast.success('Enlace copiado.'))
      .catch(() => this.toast.error('No fue posible copiar el enlace.'));
  }

  protected whatsAppUrl(groupName: string, url: string): string {
    const text = `Hola ${groupName}, te compartimos la invitación privada del evento: ${url}`;
    return `https://wa.me/?text=${encodeURIComponent(text)}`;
  }

  protected previewValue(value: string): string {
    return value.replace(
      '{{group.displayName}}',
      this.dashboard()?.groups[0]?.displayName ?? 'Tu grupo',
    );
  }

  protected visibilityLabel(block: InvitationBlock): string {
    return (
      (
        {
          Everyone: 'Todas las invitaciones',
          InvitationGroup: 'Grupo específico',
          HasTag: 'Por etiqueta',
          GuestType: 'Por tipo',
          VipOnly: 'Solo VIP',
        } as Record<string, string>
      )[block.visibility] ?? block.visibility
    );
  }

  protected statusLabel(status: string): string {
    return (
      (
        {
          Draft: 'Borrador',
          InReview: 'En revisión',
          ChangesRequested: 'Cambios solicitados',
          Approved: 'Aprobado',
          Published: 'Publicado',
        } as Record<string, string>
      )[status] ?? status
    );
  }

  protected blockLabel(type: InvitationBlockType): string {
    return (
      {
        Cover: 'Portada',
        Greeting: 'Saludo',
        Participants: 'Participantes',
        EventDate: 'Fecha del evento',
        Countdown: 'Cuenta regresiva',
        Story: 'Historia',
        Image: 'Imagen',
        GalleryPreview: 'Vista de galería',
        Text: 'Texto',
        Divider: 'Separador',
        DressCode: 'Vestimenta',
        Contact: 'Contacto',
        CustomButton: 'Botón',
        Footer: 'Pie',
      } as Record<InvitationBlockType, string>
    )[type];
  }

  protected fieldLabel(field: string): string {
    return (
      (
        {
          eyebrow: 'Antetítulo',
          title: 'Título',
          subtitle: 'Subtítulo',
          heading: 'Encabezado',
          body: 'Texto',
          completedText: 'Texto al terminar',
          format: 'Formato',
          dateFormat: 'Formato de fecha',
          caption: 'Pie',
          alt: 'Texto alternativo',
          text: 'Texto',
          style: 'Estilo',
          value: 'Valor',
          details: 'Detalles',
          name: 'Nombre',
          phone: 'Teléfono',
          email: 'Correo',
          label: 'Etiqueta',
        } as Record<string, string>
      )[field] ?? field
    );
  }

  protected saveStateLabel(): string {
    return (
      {
        saved: 'Guardado',
        pending: 'Cambios pendientes',
        saving: 'Guardando…',
        error: 'Error al guardar',
      } as const
    )[this.saveState()];
  }

  private load(): void {
    forkJoin({
      templates: this.api.getInvitationTemplates(this.organizationId, this.eventId),
      designs: this.api.getInvitationDesigns(this.organizationId, this.eventId),
      experience: this.api.getGuestExperience(this.organizationId, this.eventId),
      dashboard: this.api.getGuestDashboard(this.organizationId, this.eventId),
      links: this.api.getGuestLinks(this.organizationId, this.eventId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ templates, designs, experience, dashboard, links }) => {
          this.templates.set(templates);
          this.designs.set(designs);
          this.experience.set(experience);
          this.hydrateExperience(experience);
          this.dashboard.set(dashboard);
          this.links.set(links);
          if (designs[0]) this.hydrateDesign(designs[0]);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private hydrateDesign(design: InvitationDesign): void {
    this.hydrating = true;
    this.activeDesign.set(design);
    this.selectedBlockId.set(design.blocks[0]?.id ?? null);
    this.themeForm.patchValue(design.theme, { emitEvent: false });
    this.hydrating = false;
    this.saveState.set('saved');
  }

  private hydrateExperience(experience: GuestExperience): void {
    this.experienceForm.reset(
      {
        language: experience.language,
        publicTitle: experience.publicTitle,
        celebrantDisplayName: experience.celebrantDisplayName,
        welcomeMessage: experience.welcomeMessage ?? '',
        closingMessage: experience.closingMessage ?? '',
        showEventName: experience.showEventName,
        showEventDate: experience.showEventDate,
        showParticipantNames: experience.showParticipantNames,
        showCity: experience.showCity,
        privateAccessOnly: experience.privateAccessOnly,
      },
      { emitEvent: false },
    );
  }

  private buildTheme(base: InvitationTheme): InvitationTheme {
    return { ...base, ...this.themeForm.getRawValue() };
  }

  private updateBlock(id: string, transform: (block: InvitationBlock) => InvitationBlock): void {
    const design = this.activeDesign();
    if (!design) return;
    this.activeDesign.set({
      ...design,
      blocks: design.blocks.map((block) => (block.id === id ? transform(block) : block)),
    });
    this.queueAutosave();
  }

  private queueAutosave(): void {
    this.saveState.set('pending');
    if (this.autosaveHandle) clearTimeout(this.autosaveHandle);
    this.autosaveHandle = setTimeout(() => this.saveNow(), 800);
  }

  private saveNow(afterSave?: () => void): void {
    const design = this.activeDesign();
    if (!design || this.saveState() === 'saving') return;
    this.saveState.set('saving');
    this.api
      .updateInvitationDesign(this.organizationId, this.eventId, design.id, {
        name: design.name,
        theme: design.theme,
        blocks: design.blocks,
      })
      .pipe(
        finalize(() => {
          if (this.saveState() === 'saving') this.saveState.set('saved');
        }),
      )
      .subscribe({
        next: (updated) => {
          this.replaceDesign(updated);
          this.saveState.set('saved');
          afterSave?.();
        },
        error: (error: unknown) => {
          this.saveState.set('error');
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  private replaceDesign(updated: InvitationDesign): void {
    this.designs.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
    this.hydrateDesign(updated);
  }

  private review(
    versionId: string,
    action: 'comments' | 'approve' | 'request-changes',
    message: string,
  ): void {
    const design = this.activeDesign();
    if (!design) return;
    this.api
      .reviewInvitationDesign(
        this.organizationId,
        this.eventId,
        design.id,
        versionId,
        action,
        message,
      )
      .subscribe({
        next: (updated) => {
          this.replaceDesign(updated);
          this.commentForm.reset({ message: '' });
          this.toast.success(
            action === 'approve'
              ? 'Versión aprobada.'
              : action === 'request-changes'
                ? 'Cambios solicitados.'
                : 'Comentario agregado.',
          );
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private defaultContent(
    type: InvitationBlockType,
  ): Record<string, string | number | boolean | null> {
    const content: Record<InvitationBlockType, Record<string, string | number | boolean | null>> = {
      Cover: {
        eyebrow: 'Estás invitado',
        title: 'Nuestro evento',
        subtitle: '{{group.displayName}}',
      },
      Greeting: { title: 'Bienvenidos', body: 'Preparamos este momento para compartirlo contigo.' },
      Participants: { heading: 'Esta invitación incluye a', format: 'list' },
      EventDate: { heading: 'Fecha del evento', dateFormat: 'long', showTimeZone: true },
      Countdown: { heading: 'Faltan', completedText: 'El gran día llegó' },
      Story: { heading: 'Nuestra historia', body: 'Comparte aquí una historia breve.' },
      Image: { url: '', alt: 'Imagen del evento', caption: '' },
      GalleryPreview: { heading: 'Momentos', itemCount: 3 },
      Text: { body: 'Escribe un mensaje para tus invitados.' },
      Divider: { style: 'line' },
      DressCode: { heading: 'Código de vestimenta', value: 'Por definir', details: '' },
      Contact: { heading: 'Contacto', name: '', phone: '', email: '' },
      CustomButton: { label: 'Más información', url: 'https://example.com' },
      Footer: { text: 'Invitación privada creada con Plannyt' },
    };
    return content[type];
  }
}

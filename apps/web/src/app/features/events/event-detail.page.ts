import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin, of } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  ClientListItem,
  DocumentResponse,
  EventAccess,
  EventAccessRole,
  EventClient,
  EventClientRelationshipType,
  EventParticipant,
  EventResponse,
  EventStatus,
  InvitationCreated,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

type EventTab = 'overview' | 'clients' | 'participants' | 'access' | 'documents';

@Component({
  selector: 'app-event-detail-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <a class="back-link" routerLink="/app/events">← Volver a eventos</a>
      @if (loading()) {
        <div class="skeleton skeleton--hero"></div>
      } @else if (event(); as currentEvent) {
        <header class="event-hero">
          <div>
            <div class="event-hero__meta">
              <span class="eyebrow">{{ currentEvent.eventType }}</span>
              <span class="status-chip" [attr.data-status]="currentEvent.status">
                {{ statusLabel(currentEvent.status) }}
              </span>
            </div>
            <h1>{{ currentEvent.name }}</h1>
            <p>
              {{ currentEvent.startDateTime | date: 'fullDate' }} · {{ currentEvent.city }},
              {{ currentEvent.countryCode }}
            </p>
          </div>
          <div class="page-header__actions">
            @if (organization.hasPermission('guests.view')) {
              <a class="btn btn--primary" [routerLink]="['/app/events', eventId, 'guests']">
                Invitados
              </a>
            }
            @if (organization.hasPermission('invitation-designs.view')) {
              <a class="btn btn--secondary" [routerLink]="['/app/events', eventId, 'invitations']">
                Invitación digital
              </a>
            }
            @if (organization.hasPermission('contracts.view')) {
              <a class="btn btn--primary" [routerLink]="['/app/events', eventId, 'contracting']">
                Contratación
              </a>
            }
            @if (organization.hasPermission('events.update')) {
              <a class="btn btn--secondary" [routerLink]="['/app/events', eventId, 'edit']">
                Editar detalles
              </a>
            }
          </div>
        </header>

        <nav class="tabs" aria-label="Secciones del evento">
          @for (tab of visibleTabs(); track tab.id) {
            <button
              type="button"
              [class.is-active]="activeTab() === tab.id"
              (click)="activeTab.set(tab.id)"
            >
              {{ tab.label }}
              @if (tab.count !== null) {
                <span>{{ getTabCount(tab.id) }}</span>
              }
            </button>
          }
        </nav>

        @switch (activeTab()) {
          @case ('overview') {
            <section class="detail-grid">
              <article class="card card--padded">
                <div class="section-heading">
                  <div>
                    <span class="eyebrow">Información compartida</span>
                    <h2>Resumen</h2>
                  </div>
                </div>
                <dl class="detail-list">
                  <div>
                    <dt>Inicio</dt>
                    <dd>{{ currentEvent.startDateTime | date: 'medium' }}</dd>
                  </div>
                  <div>
                    <dt>Fin</dt>
                    <dd>
                      {{
                        currentEvent.endDateTime
                          ? (currentEvent.endDateTime | date: 'medium')
                          : 'Sin definir'
                      }}
                    </dd>
                  </div>
                  <div>
                    <dt>Zona horaria</dt>
                    <dd>{{ currentEvent.timeZone }}</dd>
                  </div>
                  <div>
                    <dt>Invitados estimados</dt>
                    <dd>{{ currentEvent.estimatedGuestCount ?? 'Sin estimación' }}</dd>
                  </div>
                </dl>
                <div class="shared-copy">
                  <strong>Descripción para el portal</strong>
                  <p>
                    {{ currentEvent.sharedDescription ?? 'Aún no hay descripción compartida.' }}
                  </p>
                </div>
              </article>

              <aside class="card card--padded">
                <span class="eyebrow">Flujo del evento</span>
                <h2>Cambiar estado</h2>
                @if (organization.hasPermission('events.update')) {
                  <form
                    [formGroup]="statusForm"
                    (ngSubmit)="changeStatus()"
                    class="form-stack compact-form"
                  >
                    <label>
                      Nuevo estado
                      <select formControlName="newStatus">
                        @for (status of statuses; track status) {
                          <option [value]="status">{{ statusLabel(status) }}</option>
                        }
                      </select>
                    </label>
                    <label>
                      Motivo
                      <textarea formControlName="reason" rows="3"></textarea>
                    </label>
                    <button class="btn btn--secondary btn--full" type="submit">
                      Aplicar transición
                    </button>
                  </form>
                }
                <div class="timeline">
                  @for (history of currentEvent.statusHistory; track history.id) {
                    <div class="timeline__item">
                      <span></span>
                      <div>
                        <strong>
                          {{ statusLabel(history.previousStatus) }} →
                          {{ statusLabel(history.newStatus) }}
                        </strong>
                        <small>{{ history.changedAt | date: 'medium' }}</small>
                        @if (history.reason) {
                          <p>{{ history.reason }}</p>
                        }
                      </div>
                    </div>
                  } @empty {
                    <p class="muted">Aún no hay transiciones registradas.</p>
                  }
                </div>
              </aside>
            </section>
          }

          @case ('clients') {
            <section class="detail-grid">
              <article class="card card--padded">
                <div class="section-heading">
                  <div>
                    <span class="eyebrow">Relaciones</span>
                    <h2>Clientes del evento</h2>
                  </div>
                </div>
                @for (relation of eventClients(); track relation.id) {
                  <div class="contact-row">
                    <span class="avatar avatar--soft">
                      {{ relation.clientDisplayName.charAt(0) }}
                    </span>
                    <span>
                      <strong>{{ relation.clientDisplayName }}</strong>
                      <small>{{ relationshipLabel(relation.relationshipType) }}</small>
                    </span>
                    @if (relation.isPrimary) {
                      <span class="status-chip">Principal</span>
                    }
                    @if (organization.hasPermission('events.update')) {
                      <button
                        class="icon-button icon-button--danger"
                        type="button"
                        aria-label="Quitar relación"
                        (click)="removeClient(relation)"
                      >
                        ×
                      </button>
                    }
                  </div>
                } @empty {
                  <div class="empty-state">
                    <h3>Sin clientes relacionados</h3>
                    <p>Vincula al cliente contratante o principal.</p>
                  </div>
                }
              </article>
              @if (
                organization.hasPermission('events.update') &&
                organization.hasPermission('clients.view')
              ) {
                <aside class="card card--padded">
                  <span class="eyebrow">Nueva relación</span>
                  <h2>Vincular cliente</h2>
                  <form
                    [formGroup]="clientForm"
                    (ngSubmit)="addClient()"
                    class="form-stack compact-form"
                  >
                    <label>
                      Cliente
                      <select formControlName="clientId">
                        <option value="">Selecciona un cliente</option>
                        @for (client of clientCatalog(); track client.id) {
                          <option [value]="client.id">{{ client.displayName }}</option>
                        }
                      </select>
                    </label>
                    <label>
                      Relación
                      <select formControlName="relationshipType">
                        <option value="ContractingClient">Contratante</option>
                        <option value="PrimaryClient">Principal</option>
                        <option value="Payer">Pagador</option>
                        <option value="Approver">Aprobador</option>
                        <option value="Other">Otro</option>
                      </select>
                    </label>
                    <label class="checkbox-row">
                      <input type="checkbox" formControlName="isPrimary" />
                      <span>Cliente principal del evento</span>
                    </label>
                    <label class="checkbox-row">
                      <input type="checkbox" formControlName="hasTransferAuthority" />
                      <span>Puede autorizar transferencias futuras</span>
                    </label>
                    <button
                      class="btn btn--secondary btn--full"
                      type="submit"
                      [disabled]="clientForm.invalid"
                    >
                      Vincular cliente
                    </button>
                  </form>
                </aside>
              }
            </section>
          }

          @case ('participants') {
            <section class="detail-grid">
              <article class="card card--padded">
                <div class="section-heading">
                  <div>
                    <span class="eyebrow">Protagonistas</span>
                    <h2>Participantes</h2>
                  </div>
                </div>
                @for (participant of participants(); track participant.id) {
                  <div class="contact-row">
                    <span class="avatar avatar--soft">{{ participant.displayName.charAt(0) }}</span>
                    <span>
                      <strong>{{ participant.displayName }}</strong>
                      <small>{{ participant.participantType }}</small>
                    </span>
                    <span
                      class="status-chip"
                      [class.status-chip--muted]="!participant.isVisibleToClient"
                    >
                      {{ participant.isVisibleToClient ? 'Visible' : 'Interno' }}
                    </span>
                  </div>
                } @empty {
                  <div class="empty-state">
                    <h3>Sin participantes</h3>
                    <p>Agrega a quienes protagonizan este evento.</p>
                  </div>
                }
              </article>
              @if (organization.hasPermission('participants.manage')) {
                <aside class="card card--padded">
                  <span class="eyebrow">Nuevo participante</span>
                  <h2>Agregar persona</h2>
                  <form
                    [formGroup]="participantForm"
                    (ngSubmit)="addParticipant()"
                    class="form-grid compact-form"
                  >
                    <label>Nombre <input formControlName="firstName" /></label>
                    <label>Apellido <input formControlName="lastName" /></label>
                    <label
                      >Tipo <input formControlName="participantType" placeholder="Homenajeado"
                    /></label>
                    <label
                      >Orden <input type="number" min="0" formControlName="displayOrder"
                    /></label>
                    <label class="span-2"
                      >Correo <input type="email" formControlName="contactEmail"
                    /></label>
                    <label class="checkbox-row span-2">
                      <input type="checkbox" formControlName="isVisibleToClient" />
                      <span>Mostrar en el portal del cliente</span>
                    </label>
                    <label class="span-2">
                      Descripción compartida
                      <textarea rows="2" formControlName="sharedDescription"></textarea>
                    </label>
                    <button
                      class="btn btn--secondary span-2"
                      type="submit"
                      [disabled]="participantForm.invalid"
                    >
                      Agregar participante
                    </button>
                  </form>
                </aside>
              }
            </section>
          }

          @case ('access') {
            <section class="detail-grid">
              <article class="card card--padded">
                <div class="section-heading">
                  <div>
                    <span class="eyebrow">Portal del cliente</span>
                    <h2>Accesos activos</h2>
                  </div>
                </div>
                @for (access of accesses(); track access.id) {
                  <div class="contact-row">
                    <span class="avatar avatar--soft">{{
                      access.email.charAt(0).toUpperCase()
                    }}</span>
                    <span>
                      <strong>{{ access.email }}</strong>
                      <small>{{ roleLabel(access.role) }}</small>
                    </span>
                    <span class="status-chip" [attr.data-status]="access.status">{{
                      access.status
                    }}</span>
                    @if (
                      access.status !== 'Revoked' &&
                      organization.hasPermission('events.members.revoke')
                    ) {
                      <button
                        class="btn btn--danger-quiet"
                        type="button"
                        (click)="revokeAccess(access)"
                      >
                        Revocar
                      </button>
                    }
                  </div>
                } @empty {
                  <div class="empty-state">
                    <h3>Nadie tiene acceso todavía</h3>
                    <p>Crea un enlace y compártelo manualmente con el cliente.</p>
                  </div>
                }
              </article>
              @if (organization.hasPermission('events.members.invite')) {
                <aside class="card card--padded">
                  <span class="eyebrow">Invitación segura</span>
                  <h2>Crear enlace</h2>
                  <form
                    [formGroup]="inviteForm"
                    (ngSubmit)="invite()"
                    class="form-stack compact-form"
                  >
                    <label>
                      Correo objetivo
                      <input type="email" formControlName="targetEmail" />
                    </label>
                    <label>
                      Rol
                      <select formControlName="role">
                        <option value="ClientPrimary">Cliente principal</option>
                        <option value="ClientAuthority">Autoridad del cliente</option>
                        <option value="ClientPayer">Pagador</option>
                        <option value="ClientApprover">Aprobador</option>
                        <option value="ClientGuestManager">Gestor de invitados</option>
                        <option value="ClientCollaborator">Colaborador</option>
                        <option value="ClientViewer">Observador</option>
                      </select>
                    </label>
                    <button
                      class="btn btn--primary btn--full"
                      type="submit"
                      [disabled]="inviteForm.invalid"
                    >
                      Generar invitación
                    </button>
                  </form>
                  @if (invitationUrl()) {
                    <div class="copy-box">
                      <p>El enlace se muestra una sola vez.</p>
                      <code>{{ invitationUrl() }}</code>
                      <button
                        class="btn btn--secondary btn--full"
                        type="button"
                        (click)="copyInvitation()"
                      >
                        Copiar enlace
                      </button>
                      <button
                        class="btn btn--danger-quiet btn--full"
                        type="button"
                        (click)="revokeInvitation()"
                      >
                        Revocar enlace
                      </button>
                    </div>
                  }
                </aside>
              }
            </section>
          }

          @case ('documents') {
            <section class="detail-grid">
              <article class="card card--padded">
                <div class="section-heading">
                  <div>
                    <span class="eyebrow">Archivos autorizados</span>
                    <h2>Documentos</h2>
                  </div>
                </div>
                @for (document of documents(); track document.id) {
                  <div class="document-row">
                    <span class="document-row__icon">{{
                      document.mimeType === 'application/pdf' ? 'PDF' : 'IMG'
                    }}</span>
                    <span>
                      <strong>{{ document.fileName }}</strong>
                      <small>
                        {{ document.documentType }} · {{ formatBytes(document.sizeBytes) }}
                      </small>
                    </span>
                    <span class="status-chip">
                      {{ document.visibility === 'ClientShared' ? 'Compartido' : 'Interno' }}
                    </span>
                    <button
                      class="icon-button"
                      type="button"
                      aria-label="Descargar"
                      (click)="downloadDocument(document)"
                    >
                      ↓
                    </button>
                    @if (organization.hasPermission('documents.delete')) {
                      <button
                        class="icon-button icon-button--danger"
                        type="button"
                        aria-label="Eliminar"
                        (click)="deleteDocument(document)"
                      >
                        ×
                      </button>
                    }
                  </div>
                } @empty {
                  <div class="empty-state">
                    <h3>Sin documentos</h3>
                    <p>Sube un PDF, JPEG o PNG de hasta 10 MB.</p>
                  </div>
                }
              </article>
              @if (
                organization.hasPermission('documents.upload-internal') ||
                organization.hasPermission('documents.upload-shared')
              ) {
                <aside class="card card--padded">
                  <span class="eyebrow">Nuevo documento</span>
                  <h2>Subir archivo</h2>
                  <div class="form-stack compact-form">
                    <label>
                      Tipo de documento
                      <input
                        [value]="documentType()"
                        (input)="updateDocumentType($event)"
                        placeholder="Contrato"
                      />
                    </label>
                    <label>
                      Visibilidad
                      <select
                        [value]="documentVisibility()"
                        (change)="updateDocumentVisibility($event)"
                      >
                        <option value="Internal">Solo equipo</option>
                        <option value="ClientShared">Compartido con cliente</option>
                      </select>
                    </label>
                    <label class="file-drop">
                      <input
                        type="file"
                        accept=".pdf,.jpg,.jpeg,.png"
                        (change)="selectFile($event)"
                      />
                      <span>Seleccionar PDF, JPEG o PNG</span>
                      <small>{{ selectedFile()?.name ?? 'Máximo 10 MB' }}</small>
                    </label>
                    <button
                      class="btn btn--primary btn--full"
                      type="button"
                      [disabled]="
                        !selectedFile() || !documentType() || uploading() || !canUploadDocument()
                      "
                      (click)="uploadDocument()"
                    >
                      {{ uploading() ? 'Subiendo…' : 'Subir documento' }}
                    </button>
                  </div>
                </aside>
              }
            </section>
          }
        }
      }
    </div>
  `,
})
export class EventDetailPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly eventId =
    this.route.snapshot.paramMap.get('id') ??
    (() => {
      throw new Error('La ruta requiere un evento.');
    })();
  protected readonly loading = signal(true);
  protected readonly activeTab = signal<EventTab>('overview');
  protected readonly event = signal<EventResponse | null>(null);
  protected readonly clientCatalog = signal<ClientListItem[]>([]);
  protected readonly eventClients = signal<EventClient[]>([]);
  protected readonly participants = signal<EventParticipant[]>([]);
  protected readonly accesses = signal<EventAccess[]>([]);
  protected readonly documents = signal<DocumentResponse[]>([]);
  protected readonly invitation = signal<InvitationCreated | null>(null);
  protected readonly invitationUrl = computed(() => this.invitation()?.invitationUrl ?? null);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly documentType = signal('Contrato');
  protected readonly documentVisibility = signal('Internal');
  protected readonly uploading = signal(false);
  protected readonly tabs: ReadonlyArray<{
    id: EventTab;
    label: string;
    count: number | null;
  }> = [
    { id: 'overview', label: 'Resumen', count: null },
    { id: 'clients', label: 'Clientes', count: 0 },
    { id: 'participants', label: 'Participantes', count: 0 },
    { id: 'access', label: 'Accesos', count: 0 },
    { id: 'documents', label: 'Documentos', count: 0 },
  ];
  protected readonly visibleTabs = computed(() =>
    this.tabs.filter((tab) => {
      switch (tab.id) {
        case 'participants':
          return this.organization.hasPermission('participants.view');
        case 'access':
          return this.organization.hasPermission('events.members.view');
        case 'documents':
          return (
            this.organization.hasPermission('documents.view-internal') ||
            this.organization.hasPermission('documents.view-shared')
          );
        case 'overview':
        case 'clients':
          return true;
      }
    }),
  );
  protected readonly statuses: EventStatus[] = [
    'Preliminary',
    'Confirmed',
    'Planning',
    'Suspended',
    'Cancelled',
    'Closed',
    'Archived',
  ];
  protected readonly statusForm = new FormGroup({
    newStatus: new FormControl<EventStatus>('Confirmed', { nonNullable: true }),
    reason: new FormControl('', { nonNullable: true }),
  });
  protected readonly clientForm = new FormGroup({
    clientId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    relationshipType: new FormControl<EventClientRelationshipType>('ContractingClient', {
      nonNullable: true,
    }),
    isPrimary: new FormControl(false, { nonNullable: true }),
    hasTransferAuthority: new FormControl(false, { nonNullable: true }),
  });
  protected readonly participantForm = new FormGroup({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    contactEmail: new FormControl('', {
      nonNullable: true,
      validators: [Validators.email],
    }),
    contactPhone: new FormControl('', { nonNullable: true }),
    preferredLanguage: new FormControl('es', { nonNullable: true }),
    timeZone: new FormControl(
      Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Matamoros',
      { nonNullable: true },
    ),
    participantType: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    displayOrder: new FormControl(0, { nonNullable: true }),
    isVisibleToClient: new FormControl(true, { nonNullable: true }),
    sharedDescription: new FormControl('', { nonNullable: true }),
  });
  protected readonly inviteForm = new FormGroup({
    targetEmail: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    role: new FormControl<EventAccessRole>('ClientPrimary', {
      nonNullable: true,
    }),
  });

  constructor() {
    this.load();
  }

  protected getTabCount(tab: EventTab): number {
    switch (tab) {
      case 'clients':
        return this.eventClients().length;
      case 'participants':
        return this.participants().length;
      case 'access':
        return this.accesses().filter((access) => access.status !== 'Revoked').length;
      case 'documents':
        return this.documents().length;
      case 'overview':
        return 0;
    }
  }

  protected changeStatus(): void {
    const value = this.statusForm.getRawValue();
    this.api
      .changeEventStatus(
        this.organization.requireOrganizationId(),
        this.eventId,
        value.newStatus,
        value.reason || null,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (event) => {
          this.event.set(event);
          this.toast.success('Estado actualizado.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected addClient(): void {
    if (this.clientForm.invalid) {
      return;
    }

    this.api
      .addEventClient(
        this.organization.requireOrganizationId(),
        this.eventId,
        this.clientForm.getRawValue(),
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (relation) => {
          this.eventClients.update((items) => [...items, relation]);
          this.clientForm.patchValue({ clientId: '', isPrimary: false });
          this.toast.success('Cliente vinculado.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected removeClient(relation: EventClient): void {
    if (!window.confirm('¿Quitar esta relación del evento?')) {
      return;
    }

    this.api
      .removeEventClient(this.organization.requireOrganizationId(), this.eventId, relation.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () =>
          this.eventClients.update((items) => items.filter((item) => item.id !== relation.id)),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected addParticipant(): void {
    if (this.participantForm.invalid) {
      return;
    }

    const value = this.participantForm.getRawValue();
    this.api
      .addParticipant(this.organization.requireOrganizationId(), this.eventId, {
        ...value,
        contactEmail: value.contactEmail || null,
        contactPhone: value.contactPhone || null,
        sharedDescription: value.sharedDescription || null,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (participant) => {
          this.participants.update((items) => [...items, participant]);
          this.participantForm.patchValue({
            firstName: '',
            lastName: '',
            contactEmail: '',
            contactPhone: '',
            participantType: '',
            sharedDescription: '',
            displayOrder: this.participants().length,
          });
          this.toast.success('Participante agregado.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected invite(): void {
    if (this.inviteForm.invalid) {
      return;
    }

    const value = this.inviteForm.getRawValue();
    this.api
      .inviteEventAccess(
        this.organization.requireOrganizationId(),
        this.eventId,
        value.targetEmail,
        value.role,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invitation) => {
          this.invitation.set(invitation);
          this.toast.success('Invitación creada. Copia el enlace ahora.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected async copyInvitation(): Promise<void> {
    const url = this.invitationUrl();
    if (!url) {
      return;
    }

    try {
      await navigator.clipboard.writeText(url);
      this.toast.success('Enlace copiado.');
    } catch {
      this.toast.error('No fue posible copiar. Selecciona el enlace manualmente.');
    }
  }

  protected revokeInvitation(): void {
    const invitation = this.invitation();
    if (!invitation || !window.confirm('¿Revocar este enlace de invitación?')) {
      return;
    }

    this.api
      .revokeEventInvitation(this.organization.requireOrganizationId(), this.eventId, invitation.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.invitation.set(null);
          this.toast.success('Invitación revocada.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected revokeAccess(access: EventAccess): void {
    if (!window.confirm(`¿Revocar el acceso de ${access.email}?`)) {
      return;
    }

    this.api
      .revokeEventAccess(this.organization.requireOrganizationId(), this.eventId, access.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.accesses.update((items) =>
            items.map((item) => (item.id === access.id ? { ...item, status: 'Revoked' } : item)),
          );
          this.toast.success('Acceso revocado.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected updateDocumentType(event: Event): void {
    this.documentType.set((event.target as HTMLInputElement).value);
  }

  protected updateDocumentVisibility(event: Event): void {
    this.documentVisibility.set((event.target as HTMLSelectElement).value);
  }

  protected selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.item(0) ?? null);
  }

  protected uploadDocument(): void {
    const file = this.selectedFile();
    if (!file || !this.documentType() || this.uploading()) {
      return;
    }

    this.uploading.set(true);
    this.api
      .uploadDocument(
        this.organization.requireOrganizationId(),
        this.eventId,
        file,
        this.documentType(),
        this.documentVisibility(),
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.uploading.set(false)),
      )
      .subscribe({
        next: (document) => {
          this.documents.update((items) => [document, ...items]);
          this.selectedFile.set(null);
          this.toast.success('Documento subido.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected downloadDocument(document: DocumentResponse): void {
    this.api
      .downloadAdminDocument(this.organization.requireOrganizationId(), this.eventId, document.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => this.saveBlob(blob, document.fileName),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected deleteDocument(document: DocumentResponse): void {
    if (!window.confirm(`¿Eliminar ${document.fileName}?`)) {
      return;
    }

    this.api
      .deleteDocument(this.organization.requireOrganizationId(), this.eventId, document.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.documents.update((items) => items.filter((item) => item.id !== document.id));
          this.toast.success('Documento eliminado.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected formatBytes(size: number): string {
    return size >= 1024 * 1024
      ? `${(size / (1024 * 1024)).toFixed(1)} MB`
      : `${Math.ceil(size / 1024)} KB`;
  }

  protected canUploadDocument(): boolean {
    return this.documentVisibility() === 'ClientShared'
      ? this.organization.hasPermission('documents.upload-shared')
      : this.organization.hasPermission('documents.upload-internal');
  }

  protected statusLabel(status: string): string {
    const labels: Record<string, string> = {
      Preliminary: 'Preliminar',
      Confirmed: 'Confirmado',
      Planning: 'Planeación',
      Suspended: 'Suspendido',
      Cancelled: 'Cancelado',
      Closed: 'Cerrado',
      Archived: 'Archivado',
    };
    return labels[status] ?? status;
  }

  protected relationshipLabel(type: EventClientRelationshipType): string {
    const labels: Record<EventClientRelationshipType, string> = {
      ContractingClient: 'Cliente contratante',
      PrimaryClient: 'Cliente principal',
      Payer: 'Pagador',
      Approver: 'Aprobador',
      Other: 'Otra relación',
    };
    return labels[type];
  }

  protected roleLabel(role: EventAccessRole): string {
    const labels: Record<EventAccessRole, string> = {
      ClientAuthority: 'Autoridad del cliente',
      ClientPrimary: 'Cliente principal',
      ClientCollaborator: 'Colaborador',
      ClientGuestManager: 'Gestor de invitados',
      ClientPayer: 'Pagador',
      ClientApprover: 'Aprobador',
      ClientViewer: 'Observador',
    };
    return labels[role];
  }

  private load(): void {
    const organizationId = this.organization.requireOrganizationId();
    forkJoin({
      event: this.api.getEvent(organizationId, this.eventId),
      clients: this.organization.hasPermission('clients.view')
        ? this.api.getClients(organizationId)
        : of({ items: [], page: 1, pageSize: 50, totalCount: 0 }),
      eventClients: this.api.getEventClients(organizationId, this.eventId),
      participants: this.organization.hasPermission('participants.view')
        ? this.api.getParticipants(organizationId, this.eventId)
        : of([]),
      accesses: this.organization.hasPermission('events.members.view')
        ? this.api.getEventAccesses(organizationId, this.eventId)
        : of([]),
      documents:
        this.organization.hasPermission('documents.view-internal') ||
        this.organization.hasPermission('documents.view-shared')
          ? this.api.getDocuments(organizationId, this.eventId)
          : of([]),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.event.set(result.event);
          this.clientCatalog.set(result.clients.items);
          this.eventClients.set(result.eventClients);
          this.participants.set(result.participants);
          this.accesses.set(result.accesses);
          this.documents.set(result.documents);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }
}

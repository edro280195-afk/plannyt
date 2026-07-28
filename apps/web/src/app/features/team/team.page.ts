import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import {
  InvitationCreated,
  OrganizationMember,
  OrganizationRole,
} from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-team-page',
  imports: [DatePipe, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Organización</span>
          <h1>Equipo</h1>
          <p>Accesos internos con roles y permisos controlados.</p>
        </div>
      </header>

      <section class="detail-grid">
        <article class="card card--padded">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Miembros</span>
              <h2>{{ members().length }} personas</h2>
            </div>
          </div>
          @for (member of members(); track member.membershipId) {
            <div class="contact-row">
              <span class="avatar avatar--soft">{{ member.displayName.charAt(0) }}</span>
              <span>
                <strong>{{ member.displayName }}</strong>
                <small>{{ member.email }} · desde {{ member.joinedAt | date: 'mediumDate' }}</small>
              </span>
              <span class="status-chip">{{ roleLabel(member.role) }}</span>
              @if (
                member.status === 'Active' &&
                organization.hasPermission('organization.members.revoke')
              ) {
                <button class="btn btn--danger-quiet" type="button" (click)="revoke(member)">
                  Revocar
                </button>
              }
            </div>
          }
        </article>

        @if (organization.hasPermission('organization.members.invite')) {
          <aside class="card card--padded">
            <span class="eyebrow">Invitación</span>
            <h2>Sumar al equipo</h2>
            <p class="muted">Comparte manualmente el enlace. Expira en siete días.</p>
            <form [formGroup]="form" (ngSubmit)="invite()" class="form-stack compact-form">
              <label>
                Correo
                <input type="email" formControlName="targetEmail" />
              </label>
              <label>
                Rol
                <select formControlName="role">
                  <option value="OrganizationAdmin">Administrador</option>
                  <option value="Planner">Planner</option>
                  <option value="Coordinator">Coordinación</option>
                  <option value="Assistant">Asistente</option>
                  <option value="Commercial">Comercial</option>
                  <option value="Finance">Finanzas</option>
                </select>
              </label>
              <button class="btn btn--primary btn--full" type="submit" [disabled]="form.invalid">
                Crear invitación
              </button>
            </form>
            @if (invitationUrl()) {
              <div class="copy-box">
                <code>{{ invitationUrl() }}</code>
                <button class="btn btn--secondary btn--full" type="button" (click)="copy()">
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
    </div>
  `,
})
export class TeamPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly members = signal<OrganizationMember[]>([]);
  protected readonly invitation = signal<InvitationCreated | null>(null);
  protected readonly invitationUrl = computed(() => this.invitation()?.invitationUrl ?? null);
  protected readonly form = new FormGroup({
    targetEmail: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    role: new FormControl<OrganizationRole>('Coordinator', {
      nonNullable: true,
    }),
  });

  constructor() {
    this.load();
  }

  protected invite(): void {
    if (this.form.invalid) {
      return;
    }

    const value = this.form.getRawValue();
    this.api
      .inviteMember(this.organization.requireOrganizationId(), value.targetEmail, value.role)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invitation) => {
          this.invitation.set(invitation);
          this.toast.success('Invitación creada.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected revoke(member: OrganizationMember): void {
    if (!window.confirm(`¿Revocar el acceso de ${member.displayName}?`)) {
      return;
    }

    this.api
      .revokeMember(this.organization.requireOrganizationId(), member.membershipId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.members.update((items) =>
            items.map((item) =>
              item.membershipId === member.membershipId ? { ...item, status: 'Revoked' } : item,
            ),
          );
          this.toast.success('Acceso revocado.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected async copy(): Promise<void> {
    const url = this.invitationUrl();
    if (!url) {
      return;
    }

    try {
      await navigator.clipboard.writeText(url);
      this.toast.success('Enlace copiado.');
    } catch {
      this.toast.error('No fue posible copiar el enlace.');
    }
  }

  protected revokeInvitation(): void {
    const invitation = this.invitation();
    if (!invitation || !window.confirm('¿Revocar este enlace de invitación?')) {
      return;
    }

    this.api
      .revokeOrganizationInvitation(this.organization.requireOrganizationId(), invitation.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.invitation.set(null);
          this.toast.success('Invitación revocada.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected roleLabel(role: OrganizationRole): string {
    const labels: Record<OrganizationRole, string> = {
      Owner: 'Owner',
      OrganizationAdmin: 'Administrador',
      Planner: 'Planner',
      Coordinator: 'Coordinación',
      Assistant: 'Asistente',
      Commercial: 'Comercial',
      Finance: 'Finanzas',
    };
    return labels[role];
  }

  private load(): void {
    this.api
      .getMembers(this.organization.requireOrganizationId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (members) => this.members.set(members),
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }
}

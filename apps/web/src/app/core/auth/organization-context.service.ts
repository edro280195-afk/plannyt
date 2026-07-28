import { computed, inject, Injectable } from '@angular/core';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class OrganizationContextService {
  private readonly auth = inject(AuthService);

  readonly organization = computed(() => this.auth.primaryOrganization());
  readonly organizationId = computed(() => this.organization()?.organizationId ?? null);

  requireOrganizationId(): string {
    const organizationId = this.organizationId();
    if (!organizationId) {
      throw new Error('No existe una organización activa.');
    }

    return organizationId;
  }

  hasPermission(permission: string): boolean {
    return this.organization()?.permissions.includes(permission) ?? false;
  }
}

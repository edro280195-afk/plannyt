import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { MeOrganization } from '../models/api.models';
import { AuthService } from './auth.service';
import { OrganizationContextService } from './organization-context.service';

describe('OrganizationContextService', () => {
  const organization = signal<MeOrganization | null>({
    organizationId: 'org-1',
    organizationName: 'Armonía Eventos',
    membershipId: 'membership-1',
    role: 'Owner' as const,
    permissions: ['events.read'],
  });

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        OrganizationContextService,
        {
          provide: AuthService,
          useValue: { primaryOrganization: organization.asReadonly() },
        },
      ],
    });
  });

  it('returns the active organization and its permissions', () => {
    const service = TestBed.inject(OrganizationContextService);

    expect(service.requireOrganizationId()).toBe('org-1');
    expect(service.hasPermission('events.read')).toBe(true);
    expect(service.hasPermission('events.delete')).toBe(false);
  });

  it('fails clearly when there is no active organization', () => {
    organization.set(null);
    const service = TestBed.inject(OrganizationContextService);

    expect(() => service.requireOrganizationId()).toThrowError(
      'No existe una organización activa.',
    );

    organization.set({
      organizationId: 'org-1',
      organizationName: 'Armonía Eventos',
      membershipId: 'membership-1',
      role: 'Owner',
      permissions: ['events.read'],
    });
  });
});

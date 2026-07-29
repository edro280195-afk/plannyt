import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import {
  RsvpDashboardResponse,
  RsvpSettingsResponse,
  SensitiveGuestDataResponse,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';
import { RsvpDashboardPage } from './rsvp-dashboard.page';

describe('RsvpDashboardPage', () => {
  const permissions: string[] = [];
  const dashboard: RsvpDashboardResponse = {
    totalGroups: 1,
    totalGuestsGranted: 1,
    guestsConfirmed: 1,
    guestsNotAttending: 0,
    guestsTentative: 0,
    guestsPending: 0,
    partialResponses: 0,
    changedAfterSubmission: 0,
    closesAt: null,
    groups: [
      {
        groupId: 'group-1',
        groupName: 'Familia Luna',
        status: 'Confirmed',
        confirmedCount: 1,
        declinedCount: 0,
        pendingCount: 0,
        hasMenuSelection: false,
        hasTransport: false,
        hasAccommodation: false,
        hasSensitiveData: true,
        lastResponseAt: '2026-07-29T12:00:00Z',
      },
    ],
  };
  const settings: RsvpSettingsResponse = {
    id: 'settings-1',
    status: 'Open',
    opensAt: null,
    closesAt: null,
    timeZone: 'America/Matamoros',
    allowChangesAfterSubmission: true,
    changesCloseAt: null,
    allowTentativeResponse: false,
    allowGroupDecline: true,
    requireResponseForEveryNamedGuest: true,
    requireCompanionNames: false,
    allowContactInformationUpdate: false,
    showAttendanceSummaryAfterSubmission: true,
    confirmationTitle: null,
    confirmationMessage: null,
    declineMessage: null,
    closedMessage: null,
    privacyNotice: null,
    sensitiveDataConsentText: null,
    updatedAt: '2026-07-29T12:00:00Z',
  };
  const sensitiveRecords: SensitiveGuestDataResponse[] = [
    {
      eventGuestId: 'guest-1',
      displayName: 'Elena Luna',
      allergies: 'Nuez',
      dietaryRestrictions: 'Sin lácteos',
      accessibilityRequirements: 'Rampa',
      additionalNotes: null,
      consentGrantedAt: '2026-07-29T12:00:00Z',
      updatedAt: '2026-07-29T12:00:00Z',
    },
  ];
  const api = {
    getRsvpDashboard: vi.fn(() => of(dashboard)),
    getRsvpSettings: vi.fn(() => of(settings)),
    getRsvpSensitiveData: vi.fn(() => of(sensitiveRecords)),
    exportRsvpSensitiveData: vi.fn(() => of(new Blob(['csv']))),
    publishRsvpSettings: vi.fn(() => of(settings)),
    openRsvp: vi.fn(() => of(settings)),
    closeRsvp: vi.fn(() => of(settings)),
  };
  let fixture: ComponentFixture<RsvpDashboardPage>;

  beforeEach(async () => {
    permissions.splice(0);
    settings.status = 'Open';
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [RsvpDashboardPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { params: of({ id: 'event-1' }) },
        },
        {
          provide: OrganizationContextService,
          useValue: {
            requireOrganizationId: () => 'org-1',
            hasPermission: (permission: string) =>
              permissions.includes(permission),
          },
        },
        { provide: ApiService, useValue: api },
        {
          provide: ToastService,
          useValue: {
            success: vi.fn(),
            error: vi.fn(),
          },
        },
      ],
    }).compileComponents();
  });

  it('oculta indicadores y operaciones sensibles sin permisos', () => {
    fixture = TestBed.createComponent(RsvpDashboardPage);
    fixture.detectChanges();

    const content = fixture.nativeElement.textContent as string;
    expect(content).not.toContain('Ver datos sensibles');
    expect(content).not.toContain('Exportar datos sensibles');
    expect(content).not.toContain('Sensible');
    expect(api.getRsvpSensitiveData).not.toHaveBeenCalled();
  });

  it('muestra y consulta datos sensibles únicamente con permiso de lectura', () => {
    permissions.push('guest-sensitive-data.view');
    fixture = TestBed.createComponent(RsvpDashboardPage);
    fixture.detectChanges();
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    );
    const viewButton = buttons.find((button) =>
      button.textContent?.includes('Ver datos sensibles'),
    );

    expect(viewButton).toBeDefined();
    viewButton?.click();
    fixture.detectChanges();

    expect(api.getRsvpSensitiveData).toHaveBeenCalledWith(
      'org-1',
      'event-1',
    );
    const content = fixture.nativeElement.textContent as string;
    expect(content).toContain('Elena Luna');
    expect(content).toContain('Nuez');
    expect(content).not.toContain('Exportar datos sensibles');
  });

  it('muestra exportación únicamente con el permiso específico', () => {
    permissions.push('guest-sensitive-data.export');
    fixture = TestBed.createComponent(RsvpDashboardPage);
    fixture.detectChanges();

    const content = fixture.nativeElement.textContent as string;
    expect(content).toContain('Exportar datos sensibles');
    expect(content).not.toContain('Ver datos sensibles');
  });

  it('ejecuta las transiciones expuestas según el estado vigente', () => {
    settings.status = 'Draft';
    fixture = TestBed.createComponent(RsvpDashboardPage);
    fixture.detectChanges();
    (
      Array.from(
        fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
      ).find((button) =>
        button.textContent?.includes('Publicar configuración'),
      )
    )?.click();
    expect(api.publishRsvpSettings).toHaveBeenCalledWith(
      'org-1',
      'event-1',
    );
    fixture.destroy();

    settings.status = 'Ready';
    fixture = TestBed.createComponent(RsvpDashboardPage);
    fixture.detectChanges();
    (
      Array.from(
        fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
      ).find((button) => button.textContent?.includes('Abrir RSVP'))
    )?.click();
    expect(api.openRsvp).toHaveBeenCalledWith('org-1', 'event-1');
    fixture.destroy();

    settings.status = 'Open';
    fixture = TestBed.createComponent(RsvpDashboardPage);
    fixture.detectChanges();
    (
      Array.from(
        fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
      ).find((button) => button.textContent?.includes('Cerrar RSVP'))
    )?.click();
    expect(api.closeRsvp).toHaveBeenCalledWith('org-1', 'event-1');
  });

  it('inicia la exportación sensible cuando tiene el permiso', () => {
    permissions.push('guest-sensitive-data.export');
    const createObjectUrl = vi
      .spyOn(URL, 'createObjectURL')
      .mockReturnValue('blob:prueba');
    const revokeObjectUrl = vi
      .spyOn(URL, 'revokeObjectURL')
      .mockImplementation(() => undefined);
    const anchorClick = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => undefined);
    fixture = TestBed.createComponent(RsvpDashboardPage);
    fixture.detectChanges();
    (
      Array.from(
        fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
      ).find((button) =>
        button.textContent?.includes('Exportar datos sensibles'),
      )
    )?.click();

    expect(api.exportRsvpSensitiveData).toHaveBeenCalledWith(
      'org-1',
      'event-1',
    );
    expect(createObjectUrl).toHaveBeenCalled();
    expect(anchorClick).toHaveBeenCalled();
    expect(revokeObjectUrl).toHaveBeenCalledWith('blob:prueba');
  });
});

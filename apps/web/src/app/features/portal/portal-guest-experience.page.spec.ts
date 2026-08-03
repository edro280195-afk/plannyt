import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import {
  EventAccessRole,
  InvitationDesign,
  InvitationTheme,
  MeResponse,
  PortalGuestWorkspace,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';
import { PortalGuestExperiencePage } from './portal-guest-experience.page';

describe('PortalGuestExperiencePage', () => {
  const me = signal<MeResponse>({
    userAccountId: 'user-1',
    email: 'cliente@example.invalid',
    organizations: [],
    eventAccesses: [
      {
        organizationId: 'org-1',
        eventId: 'event-1',
        eventName: 'Boda QA',
        role: 'ClientApprover',
      },
    ],
  });
  const theme: InvitationTheme = {
    backgroundColor: '#ffffff',
    surfaceColor: '#ffffff',
    textColor: '#111111',
    accentColor: '#aa0000',
    headingFont: 'lora',
    bodyFont: 'inter',
    radiusToken: 'md',
    spacingToken: 'comfortable',
    coverStyle: 'centered',
    buttonStyle: 'solid',
    animation: 'Reduced',
  };
  const design: InvitationDesign = {
    id: 'design-1',
    eventId: 'event-1',
    name: 'Romántica',
    status: 'InReview',
    theme,
    blocks: [
      {
        id: 'block-1',
        type: 'Cover',
        visible: true,
        visibility: 'Everyone',
        visibilityValue: null,
        sortOrder: 0,
        content: { title: 'Boda QA', subtitle: 'Familia Luna' },
        presentation: {},
      },
    ],
    nextVersionNumber: 2,
    approvedVersionId: null,
    versions: [
      {
        id: 'version-1',
        versionNumber: 1,
        theme,
        blocks: [],
        createdAt: '2026-08-03T12:00:00Z',
        approvedAt: null,
        publishedAt: null,
      },
    ],
    comments: [],
    accessibilityWarnings: [],
    updatedAt: '2026-08-03T12:00:00Z',
  };
  const workspace: PortalGuestWorkspace = {
    eventId: 'event-1',
    groups: [
      {
        id: 'group-1',
        displayName: 'Familia Luna',
        groupType: 'Family',
        allowedGuestCount: 2,
        namedGuestCount: 1,
        allowUnnamedCompanions: false,
        maxUnnamedCompanions: 0,
      },
    ],
    guests: [
      {
        id: 'guest-1',
        invitationGroupId: 'group-1',
        firstName: 'Elena',
        lastName: 'Luna',
        guestType: 'Family',
        ageCategory: 'Adult',
        isPrimaryContact: true,
        isVip: false,
      },
    ],
    design,
  };
  const api = {
    getPortalGuestWorkspace: vi.fn(() => of(workspace)),
    getPortalGuestDuplicates: vi.fn(() => of([])),
    getPortalGuestLinks: vi.fn(() => of([])),
    reviewPortalInvitation: vi.fn(() => of(design)),
    createPortalInvitationGroup: vi.fn(),
    updatePortalInvitationGroup: vi.fn(),
    archivePortalInvitationGroup: vi.fn(),
    createPortalGuest: vi.fn(),
    updatePortalGuest: vi.fn(),
    archivePortalGuest: vi.fn(),
    analyzePortalGuestImport: vi.fn(),
    confirmPortalGuestImport: vi.fn(),
    downloadPortalGuestImportTemplate: vi.fn(),
    markPortalGuestLinkShared: vi.fn(),
  };
  let fixture: ComponentFixture<PortalGuestExperiencePage>;

  beforeEach(async () => {
    vi.clearAllMocks();
    me.update((value) => ({
      ...value,
      eventAccesses: [{ ...value.eventAccesses[0]!, role: 'ClientApprover' }],
    }));

    await TestBed.configureTestingModule({
      imports: [PortalGuestExperiencePage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'event-1' }),
            },
          },
        },
        { provide: ApiService, useValue: api },
        { provide: AuthService, useValue: { me: me.asReadonly() } },
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

  it('oculta acciones de invitados e importación para un aprobador del cliente', () => {
    fixture = TestBed.createComponent(PortalGuestExperiencePage);
    fixture.detectChanges();

    let content = fixture.nativeElement.textContent as string;
    expect(content).toContain('Familia Luna');
    expect(content).not.toContain('Importar');
    expect(content).not.toContain('Agregar o editar datos');
    expect(content).not.toContain('Crear grupo');
    expect(fixture.nativeElement.querySelector('[aria-label="Editar grupo"]')).toBeNull();

    clickButton('Diseño');
    fixture.detectChanges();

    content = fixture.nativeElement.textContent as string;
    expect(content).toContain('Aprobar versión');
    expect(content).toContain('Solicitar cambios');
  });

  it('muestra gestión de invitados sin aprobación de diseño para gestor de invitados', () => {
    setRole('ClientGuestManager');
    fixture = TestBed.createComponent(PortalGuestExperiencePage);
    fixture.detectChanges();

    let content = fixture.nativeElement.textContent as string;
    expect(content).toContain('Importar');
    expect(content).toContain('Agregar o editar datos');
    expect(content).toContain('Crear grupo');
    expect(fixture.nativeElement.querySelector('[aria-label="Editar grupo"]')).not.toBeNull();

    clickButton('Diseño');
    fixture.detectChanges();

    content = fixture.nativeElement.textContent as string;
    expect(content).toContain('Comentar');
    expect(content).not.toContain('Aprobar versión');
    expect(content).not.toContain('Solicitar cambios');
  });

  function setRole(role: EventAccessRole): void {
    me.update((value) => ({
      ...value,
      eventAccesses: [{ ...value.eventAccesses[0]!, role }],
    }));
  }

  function clickButton(label: string): void {
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button'),
    ) as HTMLButtonElement[];
    buttons.find((button) => button.textContent?.trim() === label)?.click();
  }
});

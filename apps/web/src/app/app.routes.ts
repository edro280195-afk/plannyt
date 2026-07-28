import { Routes } from '@angular/router';
import {
  authGuard,
  permissionGuard,
  portalGuard,
  professionalGuard,
} from './core/auth/auth.guards';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'auth/login',
  },
  {
    path: 'auth',
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'login',
      },
      {
        path: 'login',
        title: 'Iniciar sesión · Plannyt',
        loadComponent: () =>
          import('./features/auth/login.page').then((module) => module.LoginPage),
      },
      {
        path: 'register',
        title: 'Crear organización · Plannyt',
        loadComponent: () =>
          import('./features/auth/register.page').then((module) => module.RegisterPage),
      },
    ],
  },
  {
    path: 'accept-access/:token',
    title: 'Aceptar invitación · Plannyt',
    loadComponent: () =>
      import('./features/invitations/invitation.page').then((module) => module.InvitationPage),
  },
  {
    path: 'login',
    pathMatch: 'full',
    redirectTo: 'auth/login',
  },
  {
    path: 'register',
    pathMatch: 'full',
    redirectTo: 'auth/register',
  },
  {
    path: 'invite/:token',
    title: 'Aceptar invitación · Plannyt',
    loadComponent: () =>
      import('./features/invitations/invitation.page').then((module) => module.InvitationPage),
  },
  {
    path: 'proposal/:token',
    title: 'Propuesta privada · Plannyt',
    loadComponent: () =>
      import('./features/proposals/public-proposal.page').then(
        (module) => module.PublicProposalPage,
      ),
  },
  {
    path: 'sign/:token',
    title: 'Firma de contrato · Plannyt',
    loadComponent: () =>
      import('./features/contracts/public-signature.page').then(
        (module) => module.PublicSignaturePage,
      ),
  },
  {
    path: 'i/:token',
    title: 'Invitación privada · Plannyt',
    loadComponent: () =>
      import('./features/invitations/public-invitation.page').then(
        (module) => module.PublicInvitationPage,
      ),
  },
  {
    path: 'app',
    canActivate: [authGuard, professionalGuard],
    loadComponent: () =>
      import('./layout/professional-shell.component').then(
        (module) => module.ProfessionalShellComponent,
      ),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        title: 'Inicio · Plannyt',
        loadComponent: () =>
          import('./features/dashboard/dashboard.page').then((module) => module.DashboardPage),
      },
      {
        path: 'prospects',
        title: 'Pipeline comercial · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'prospects.view' },
        loadComponent: () =>
          import('./features/prospects/prospects.page').then((module) => module.ProspectsPage),
      },
      {
        path: 'prospects/:id',
        title: 'Prospecto · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'prospects.view' },
        loadComponent: () =>
          import('./features/prospects/prospect-detail.page').then(
            (module) => module.ProspectDetailPage,
          ),
      },
      {
        path: 'catalog',
        title: 'Catálogo comercial · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'catalog.view' },
        loadComponent: () =>
          import('./features/catalog/catalog.page').then((module) => module.CatalogPage),
      },
      {
        path: 'proposals',
        title: 'Propuestas · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'proposals.view' },
        loadComponent: () =>
          import('./features/proposals/proposals.page').then((module) => module.ProposalsPage),
      },
      {
        path: 'proposals/new',
        title: 'Nueva propuesta · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'proposals.create' },
        loadComponent: () =>
          import('./features/proposals/proposal-builder.page').then(
            (module) => module.ProposalBuilderPage,
          ),
      },
      {
        path: 'proposals/:id',
        title: 'Propuesta · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'proposals.view' },
        loadComponent: () =>
          import('./features/proposals/proposal-builder.page').then(
            (module) => module.ProposalBuilderPage,
          ),
      },
      {
        path: 'clients',
        title: 'Clientes · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'clients.view' },
        loadComponent: () =>
          import('./features/clients/clients.page').then((module) => module.ClientsPage),
      },
      {
        path: 'clients/new',
        title: 'Nuevo cliente · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'clients.create' },
        loadComponent: () =>
          import('./features/clients/client-editor.page').then((module) => module.ClientEditorPage),
      },
      {
        path: 'clients/:id',
        title: 'Cliente · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'clients.view' },
        loadComponent: () =>
          import('./features/clients/client-editor.page').then((module) => module.ClientEditorPage),
      },
      {
        path: 'events',
        title: 'Eventos · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'events.view' },
        loadComponent: () =>
          import('./features/events/events.page').then((module) => module.EventsPage),
      },
      {
        path: 'events/new',
        title: 'Nuevo evento · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'events.create' },
        loadComponent: () =>
          import('./features/events/event-editor.page').then((module) => module.EventEditorPage),
      },
      {
        path: 'events/:id/edit',
        title: 'Editar evento · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'events.update' },
        loadComponent: () =>
          import('./features/events/event-editor.page').then((module) => module.EventEditorPage),
      },
      {
        path: 'events/:id/contracting',
        title: 'Contratación del evento · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'contracts.view' },
        loadComponent: () =>
          import('./features/contracts/event-contracting.page').then(
            (module) => module.EventContractingPage,
          ),
      },
      {
        path: 'events/:id/guests',
        title: 'Invitados del evento · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'guests.view' },
        loadComponent: () =>
          import('./features/guests/guest-management.page').then(
            (module) => module.GuestManagementPage,
          ),
      },
      {
        path: 'events/:id/invitations',
        title: 'Invitación digital · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'invitation-designs.view' },
        loadComponent: () =>
          import('./features/invitations/invitation-editor.page').then(
            (module) => module.InvitationEditorPage,
          ),
      },
      {
        path: 'events/:id',
        title: 'Evento · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'events.view' },
        loadComponent: () =>
          import('./features/events/event-detail.page').then((module) => module.EventDetailPage),
      },
      {
        path: 'contracts',
        title: 'Contratos · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'contracts.view' },
        loadComponent: () =>
          import('./features/contracts/contracts.page').then((module) => module.ContractsPage),
      },
      {
        path: 'contracts/:id',
        title: 'Detalle del contrato · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'contracts.view' },
        loadComponent: () =>
          import('./features/contracts/contract-detail.page').then(
            (module) => module.ContractDetailPage,
          ),
      },
      {
        path: 'contract-templates',
        title: 'Plantillas de contrato · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'contract-templates.view' },
        loadComponent: () =>
          import('./features/contracts/contract-templates.page').then(
            (module) => module.ContractTemplatesPage,
          ),
      },
      {
        path: 'team',
        title: 'Equipo · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'organization.members.view' },
        loadComponent: () => import('./features/team/team.page').then((module) => module.TeamPage),
      },
      {
        path: 'settings',
        title: 'Configuración · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'organization.view' },
        loadComponent: () =>
          import('./features/settings/settings.page').then((module) => module.SettingsPage),
      },
    ],
  },
  {
    path: 'portal',
    canActivate: [authGuard, portalGuard],
    loadComponent: () =>
      import('./layout/portal-shell.component').then((module) => module.PortalShellComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'events',
      },
      {
        path: 'events',
        title: 'Mis eventos · Plannyt',
        loadComponent: () =>
          import('./features/portal/portal-events.page').then((module) => module.PortalEventsPage),
      },
      {
        path: 'events/:id/guest-experience',
        title: 'Invitados e invitación · Plannyt',
        loadComponent: () =>
          import('./features/portal/portal-guest-experience.page').then(
            (module) => module.PortalGuestExperiencePage,
          ),
      },
      {
        path: 'events/:id',
        title: 'Evento compartido · Plannyt',
        loadComponent: () =>
          import('./features/portal/portal-event-detail.page').then(
            (module) => module.PortalEventDetailPage,
          ),
      },
      {
        path: 'proposals',
        title: 'Mis propuestas · Plannyt',
        loadComponent: () =>
          import('./features/portal/portal-proposals.page').then(
            (module) => module.PortalProposalsPage,
          ),
      },
      {
        path: 'proposals/:id',
        title: 'Propuesta compartida · Plannyt',
        loadComponent: () =>
          import('./features/portal/portal-proposal-detail.page').then(
            (module) => module.PortalProposalDetailPage,
          ),
      },
      {
        path: 'contracts',
        title: 'Mis contratos · Plannyt',
        loadComponent: () =>
          import('./features/portal/portal-contracts.page').then(
            (module) => module.PortalContractsPage,
          ),
      },
      {
        path: 'contracts/:id',
        title: 'Contrato compartido · Plannyt',
        loadComponent: () =>
          import('./features/portal/portal-contract-detail.page').then(
            (module) => module.PortalContractDetailPage,
          ),
      },
      {
        path: 'payments',
        title: 'Mis pagos · Plannyt',
        loadComponent: () =>
          import('./features/portal/portal-payments.page').then(
            (module) => module.PortalPaymentsPage,
          ),
      },
    ],
  },
  {
    path: '**',
    title: 'Página no encontrada · Plannyt',
    loadComponent: () => import('./features/not-found.page').then((module) => module.NotFoundPage),
  },
];

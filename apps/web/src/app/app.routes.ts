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
        path: 'events/:id',
        title: 'Evento · Plannyt',
        canActivate: [permissionGuard],
        data: { permission: 'events.view' },
        loadComponent: () =>
          import('./features/events/event-detail.page').then((module) => module.EventDetailPage),
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
        path: 'events/:id',
        title: 'Evento compartido · Plannyt',
        loadComponent: () =>
          import('./features/portal/portal-event-detail.page').then(
            (module) => module.PortalEventDetailPage,
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

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { OrganizationContextService } from './organization-context.service';
import { ToastService } from '../ui/toast.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated()
    ? true
    : router.createUrlTree(['/auth/login'], {
        queryParams: { returnUrl: state.url },
      });
};

export const professionalGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.hasProfessionalAccess()) {
    return true;
  }

  if (auth.hasPortalAccess()) {
    return router.createUrlTree(['/portal/events']);
  }

  inject(ToastService).info(
    'Ya no tienes acceso activo en esta organización. Inicia sesión con otra cuenta si corresponde.',
  );
  return router.createUrlTree(['/auth/login']);
};

export const portalGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.hasPortalAccess()) {
    return true;
  }

  if (auth.hasProfessionalAccess()) {
    return router.createUrlTree(['/app/dashboard']);
  }

  inject(ToastService).info(
    'Ya no tienes acceso activo a este portal. Inicia sesión con otra cuenta si corresponde.',
  );
  return router.createUrlTree(['/auth/login']);
};

export const permissionGuard: CanActivateFn = (route) => {
  const organization = inject(OrganizationContextService);
  const router = inject(Router);
  const permission = route.data['permission'];

  return typeof permission === 'string' && organization.hasPermission(permission)
    ? true
    : router.createUrlTree(['/app/dashboard']);
};

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { OrganizationContextService } from './organization-context.service';

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
  return auth.hasProfessionalAccess() ? true : router.createUrlTree(['/portal/events']);
};

export const portalGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.hasPortalAccess() ? true : router.createUrlTree(['/app/dashboard']);
};

export const permissionGuard: CanActivateFn = (route) => {
  const organization = inject(OrganizationContextService);
  const router = inject(Router);
  const permission = route.data['permission'];

  return typeof permission === 'string' && organization.hasPermission(permission)
    ? true
    : router.createUrlTree(['/app/dashboard']);
};

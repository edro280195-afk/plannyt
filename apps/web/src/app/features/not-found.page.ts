import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="not-found-page">
      <span class="brand__mark">P</span>
      <span class="eyebrow">404</span>
      <h1>Esta página no está en el plan.</h1>
      <p>Puede que el enlace haya cambiado o ya no esté disponible.</p>
      <a class="btn btn--primary" routerLink="/">Volver a Plannyt</a>
    </main>
  `,
})
export class NotFoundPage {}

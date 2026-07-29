import { DestroyRef, inject, Injectable, InjectionToken, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SwUpdate } from '@angular/service-worker';
import { filter } from 'rxjs';

export const PWA_RELOAD = new InjectionToken<() => void>('PWA_RELOAD', {
  providedIn: 'root',
  factory: () => () => window.location.reload(),
});

@Injectable({ providedIn: 'root' })
export class PwaUpdateService {
  private readonly swUpdate = inject(SwUpdate);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reloadPage = inject(PWA_RELOAD);

  readonly updateReady = signal(false);
  readonly activating = signal(false);

  constructor() {
    if (!this.swUpdate.isEnabled) {
      return;
    }

    this.swUpdate.versionUpdates
      .pipe(
        filter((event) => event.type === 'VERSION_READY'),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.updateReady.set(true));
  }

  async activateUpdate(): Promise<void> {
    if (this.activating()) {
      return;
    }

    this.activating.set(true);
    try {
      await this.swUpdate.activateUpdate();
      this.reloadPage();
    } catch {
      this.activating.set(false);
    }
  }
}

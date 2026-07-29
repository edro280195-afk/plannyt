import { TestBed } from '@angular/core/testing';
import { SwUpdate, VersionEvent } from '@angular/service-worker';
import { Subject } from 'rxjs';
import { PwaUpdateService } from './pwa-update.service';

describe('PwaUpdateService', () => {
  let versionUpdates: Subject<VersionEvent>;
  let service: PwaUpdateService;

  beforeEach(() => {
    versionUpdates = new Subject<VersionEvent>();
    TestBed.configureTestingModule({
      providers: [
        PwaUpdateService,
        {
          provide: SwUpdate,
          useValue: {
            isEnabled: true,
            versionUpdates,
            activateUpdate: vi.fn().mockResolvedValue(true),
          },
        },
      ],
    });
    service = TestBed.inject(PwaUpdateService);
  });

  it('keeps the update banner hidden before a version is ready', () => {
    expect(service.updateReady()).toBe(false);
  });

  it('shows the update banner when the service worker reports a ready version', () => {
    versionUpdates.next({
      type: 'VERSION_READY',
      currentVersion: { hash: 'current', appData: undefined },
      latestVersion: { hash: 'latest', appData: undefined },
    });

    expect(service.updateReady()).toBe(true);
  });
});

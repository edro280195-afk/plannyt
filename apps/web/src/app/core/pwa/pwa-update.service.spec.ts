import { TestBed } from '@angular/core/testing';
import { SwUpdate, VersionEvent } from '@angular/service-worker';
import { Subject } from 'rxjs';
import { PWA_RELOAD, PwaUpdateService } from './pwa-update.service';

describe('PwaUpdateService', () => {
  let versionUpdates: Subject<VersionEvent>;
  let activateUpdate: ReturnType<typeof vi.fn>;
  let reloadPage: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    versionUpdates = new Subject<VersionEvent>();
    activateUpdate = vi.fn().mockResolvedValue(true);
    reloadPage = vi.fn();
  });

  function createService(isEnabled = true): PwaUpdateService {
    TestBed.configureTestingModule({
      providers: [
        PwaUpdateService,
        {
          provide: SwUpdate,
          useValue: {
            isEnabled,
            versionUpdates,
            activateUpdate,
          },
        },
        { provide: PWA_RELOAD, useValue: reloadPage },
      ],
    });
    return TestBed.inject(PwaUpdateService);
  }

  it('keeps the update banner hidden before a version is ready', () => {
    const service = createService();

    expect(service.updateReady()).toBe(false);
  });

  it('provides the browser reload callback by default', () => {
    TestBed.configureTestingModule({});

    expect(TestBed.inject(PWA_RELOAD)).toEqual(expect.any(Function));
  });

  it('shows the update banner when the service worker reports a ready version', () => {
    const service = createService();
    versionUpdates.next({
      type: 'VERSION_READY',
      currentVersion: { hash: 'current', appData: undefined },
      latestVersion: { hash: 'latest', appData: undefined },
    });

    expect(service.updateReady()).toBe(true);
  });

  it('does not listen for versions when the service worker is disabled', () => {
    const service = createService(false);
    versionUpdates.next({
      type: 'VERSION_READY',
      currentVersion: { hash: 'current', appData: undefined },
      latestVersion: { hash: 'latest', appData: undefined },
    });

    expect(service.updateReady()).toBe(false);
  });

  it('activates the ready version and reloads the page', async () => {
    const service = createService();

    await service.activateUpdate();

    expect(activateUpdate).toHaveBeenCalledOnce();
    expect(reloadPage).toHaveBeenCalledOnce();
    expect(service.activating()).toBe(true);
  });

  it('ignores another activation while the first one is pending', async () => {
    let completeActivation: ((value: boolean) => void) | undefined;
    activateUpdate.mockReturnValueOnce(
      new Promise<boolean>((resolve) => {
        completeActivation = resolve;
      }),
    );
    const service = createService();

    const first = service.activateUpdate();
    await service.activateUpdate();
    expect(activateUpdate).toHaveBeenCalledOnce();

    completeActivation?.(true);
    await first;
    expect(reloadPage).toHaveBeenCalledOnce();
  });

  it('allows retrying when activation fails', async () => {
    activateUpdate.mockRejectedValueOnce(new Error('Service worker unavailable'));
    const service = createService();

    await service.activateUpdate();

    expect(service.activating()).toBe(false);
    expect(reloadPage).not.toHaveBeenCalled();
  });
});

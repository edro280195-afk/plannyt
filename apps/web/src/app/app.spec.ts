import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PwaUpdateService } from './core/pwa/pwa-update.service';
import { App } from './app';

describe('App', () => {
  const updateReady = signal(false);
  const pwaUpdate = {
    updateReady,
    activating: signal(false),
    activateUpdate: vi.fn(),
  };

  beforeEach(async () => {
    updateReady.set(false);
    pwaUpdate.activating.set(false);
    pwaUpdate.activateUpdate.mockReset();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: PwaUpdateService, useValue: pwaUpdate }],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the router outlet and toast host', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
    expect(compiled.querySelector('app-toast-host')).toBeTruthy();
  });

  it('offers an explicit reload when a PWA update is ready', () => {
    updateReady.set(true);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const banner = compiled.querySelector<HTMLElement>('[role="status"]');
    const button = compiled.querySelector<HTMLButtonElement>('.update-banner button');

    expect(banner?.textContent).toContain('nueva versión');
    button?.click();
    expect(pwaUpdate.activateUpdate).toHaveBeenCalledOnce();
  });

  it('disables the update action while activation is in progress', () => {
    updateReady.set(true);
    pwaUpdate.activating.set(true);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '.update-banner button',
    );

    expect(button?.disabled).toBe(true);
    expect(button?.textContent).toContain('Actualizando');
    pwaUpdate.activating.set(false);
  });
});

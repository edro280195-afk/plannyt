import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('adds messages with consecutive identifiers', () => {
    service.success('Guardado');
    service.error('Falló');

    expect(service.messages()).toEqual([
      { id: 1, kind: 'success', text: 'Guardado' },
      { id: 2, kind: 'error', text: 'Falló' },
    ]);
  });

  it('dismisses a selected message', () => {
    service.info('Información');

    service.dismiss(1);

    expect(service.messages()).toEqual([]);
  });

  it('automatically dismisses messages after 4.5 seconds', () => {
    service.success('Temporal');

    vi.advanceTimersByTime(4500);

    expect(service.messages()).toEqual([]);
  });
});

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../core/ui/toast.service';

@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toast-stack" aria-live="polite" aria-atomic="false">
      @for (message of toast.messages(); track message.id) {
        <button
          type="button"
          class="toast"
          [class]="'toast toast--' + message.kind"
          (click)="toast.dismiss(message.id)"
          [attr.aria-label]="'Cerrar mensaje: ' + message.text"
        >
          <span class="toast__dot"></span>
          <span>{{ message.text }}</span>
        </button>
      }
    </div>
  `,
})
export class ToastHostComponent {
  protected readonly toast = inject(ToastService);
}

import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: number;
  kind: 'success' | 'error' | 'info';
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  readonly messages = signal<ToastMessage[]>([]);

  success(text: string): void {
    this.show('success', text);
  }

  error(text: string): void {
    this.show('error', text);
  }

  info(text: string): void {
    this.show('info', text);
  }

  dismiss(id: number): void {
    this.messages.update((messages) => messages.filter((message) => message.id !== id));
  }

  private show(kind: ToastMessage['kind'], text: string): void {
    const id = this.nextId++;
    this.messages.update((messages) => [...messages, { id, kind, text }]);
    window.setTimeout(() => this.dismiss(id), 4500);
  }
}

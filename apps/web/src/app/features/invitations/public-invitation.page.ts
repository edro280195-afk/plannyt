import { DatePipe, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { InvitationBlock, PublicInvitation } from '../../core/models/api.models';

type PublicInvitationState =
  | 'loading'
  | 'available'
  | 'invalid'
  | 'expired'
  | 'revoked'
  | 'replaced'
  | 'suspended'
  | 'unpublished';

@Component({
  selector: 'app-public-invitation-page',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main
      class="public-invitation"
      [style.background]="invitation()?.theme?.backgroundColor"
      [style.color]="invitation()?.theme?.textColor"
      [style.--invite-accent]="invitation()?.theme?.accentColor"
      [style.--invite-surface]="invitation()?.theme?.surfaceColor"
      [attr.data-animation]="invitation()?.theme?.animation ?? 'Reduced'"
    >
      @if (state() === 'loading') {
        <div class="public-invitation__skeleton" aria-label="Cargando invitación">
          <span class="skeleton skeleton--hero"></span>
          <span class="skeleton"></span>
          <span class="skeleton"></span>
        </div>
      } @else if (invitation(); as invite) {
        <article class="public-invitation__sheet">
          @for (block of invite.blocks; track block.id) {
            <section class="public-invite-block" [attr.data-block]="block.type">
              @switch (block.type) {
                @case ('Cover') {
                  <div class="public-invite-cover">
                    <span class="eyebrow">{{ text(block, 'eyebrow') }}</span>
                    <h1>{{ text(block, 'title') || invite.publicTitle }}</h1>
                    <p>{{ text(block, 'subtitle') || invite.celebrantDisplayName }}</p>
                  </div>
                }
                @case ('Greeting') {
                  <h2>{{ text(block, 'title') }}</h2>
                  <p>{{ text(block, 'body') || invite.welcomeMessage }}</p>
                }
                @case ('Participants') {
                  <h2>{{ text(block, 'heading') || 'Esta invitación incluye a' }}</h2>
                  <div class="public-participants">
                    @for (guest of invite.participants; track guest.firstName + guest.lastName) {
                      <span>
                        <strong>{{ guest.firstName }} {{ guest.lastName }}</strong>
                        @if (guest.isPrimaryContact) {
                          <small>Contacto principal</small>
                        }
                        @if (guest.isVip) {
                          <small>VIP</small>
                        }
                      </span>
                    } @empty {
                      <p>Invitación para {{ invite.groupDisplayName }}.</p>
                    }
                  </div>
                  <small>Hasta {{ invite.allowedGuestCount }} personas en este grupo.</small>
                }
                @case ('EventDate') {
                  @if (invite.eventStartsAt; as eventStartsAt) {
                    <span class="eyebrow">{{ text(block, 'heading') }}</span>
                    <p class="public-event-date">{{ eventStartsAt | date: 'fullDate' }}</p>
                    <p>{{ eventStartsAt | date: 'shortTime' }} · {{ invite.eventTimeZone }}</p>
                    @if (invite.city) {
                      <small>{{ invite.city }}, {{ invite.countryCode }}</small>
                    }
                  }
                }
                @case ('Countdown') {
                  @if (invite.eventStartsAt; as eventStartsAt) {
                    <span class="eyebrow">{{ text(block, 'heading') }}</span>
                    <strong class="countdown-number">{{ daysUntil(eventStartsAt) }}</strong>
                    <p>días para el evento</p>
                  }
                }
                @case ('Story') {
                  <h2>{{ text(block, 'heading') }}</h2>
                  <p>{{ text(block, 'body') }}</p>
                }
                @case ('Image') {
                  @if (text(block, 'url')) {
                    <figure>
                      <img
                        [src]="text(block, 'url')"
                        [alt]="text(block, 'alt')"
                        loading="lazy"
                        referrerpolicy="no-referrer"
                      />
                      @if (text(block, 'caption')) {
                        <figcaption>{{ text(block, 'caption') }}</figcaption>
                      }
                    </figure>
                  }
                }
                @case ('GalleryPreview') {
                  <h2>{{ text(block, 'heading') }}</h2>
                  <div class="gallery-placeholder" aria-label="Vista previa de galería">
                    <span></span><span></span><span></span>
                  </div>
                }
                @case ('Text') {
                  <p>{{ text(block, 'body') }}</p>
                }
                @case ('Divider') {
                  <hr />
                }
                @case ('DressCode') {
                  <span class="eyebrow">{{ text(block, 'heading') }}</span>
                  <h2>{{ text(block, 'value') }}</h2>
                  <p>{{ text(block, 'details') }}</p>
                }
                @case ('Contact') {
                  <h2>{{ text(block, 'heading') }}</h2>
                  <p>{{ text(block, 'name') }}</p>
                  @if (text(block, 'phone')) {
                    <a [href]="'tel:' + text(block, 'phone')">{{ text(block, 'phone') }}</a>
                  }
                  @if (text(block, 'email')) {
                    <a [href]="'mailto:' + text(block, 'email')">{{ text(block, 'email') }}</a>
                  }
                }
                @case ('CustomButton') {
                  <a
                    class="public-invite-button"
                    [href]="text(block, 'url')"
                    target="_blank"
                    rel="noopener noreferrer nofollow"
                  >
                    {{ text(block, 'label') }}
                  </a>
                }
                @case ('Footer') {
                  <small>{{ text(block, 'text') }}</small>
                }
                @default {
                  <h2>{{ text(block, 'heading') || text(block, 'title') }}</h2>
                  <p>{{ text(block, 'body') || text(block, 'text') }}</p>
                }
              }
            </section>
          }

          <section class="public-invite-block public-rsvp-demo">
            <button type="button" disabled>Confirmar asistencia</button>
            <small>Demostración · La confirmación estará disponible próximamente</small>
          </section>
          @if (invite.closingMessage) {
            <p class="public-invite-closing">{{ invite.closingMessage }}</p>
          }
          <footer class="public-invitation__footer">
            <span class="brand__mark">P</span>
            <small>Invitación privada creada con Plannyt</small>
          </footer>
        </article>
      } @else {
        <section class="public-invitation-state">
          <span class="state-icon">{{ stateIcon() }}</span>
          <h1>{{ stateTitle() }}</h1>
          <p>{{ stateMessage() }}</p>
          <small>Solicita un enlace vigente a la persona organizadora.</small>
        </section>
      }
    </main>
  `,
})
export class PublicInvitationPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly invitation = signal<PublicInvitation | null>(null);
  protected readonly state = signal<PublicInvitationState>('loading');

  constructor() {
    this.configurePrivacyMetadata();
    const token = this.route.snapshot.paramMap.get('token') ?? '';
    this.api
      .getPublicInvitation(token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invitation) => {
          this.invitation.set(invitation);
          this.state.set('available');
          this.document.documentElement.lang = invitation.language;
        },
        error: (error: unknown) => this.state.set(this.resolveErrorState(error)),
      });
  }

  protected text(block: InvitationBlock, field: string): string {
    const value = block.content[field];
    return typeof value === 'string' ? value : '';
  }

  protected daysUntil(value: string): number {
    const milliseconds = new Date(value).getTime() - Date.now();
    return Math.max(0, Math.ceil(milliseconds / 86_400_000));
  }

  protected stateTitle(): string {
    return {
      invalid: 'Invitación no encontrada',
      expired: 'Este enlace venció',
      revoked: 'Este enlace fue revocado',
      replaced: 'Hay un enlace más reciente',
      suspended: 'Invitación temporalmente suspendida',
      unpublished: 'Invitación todavía no publicada',
      loading: 'Cargando invitación',
      available: '',
    }[this.state()];
  }

  protected stateMessage(): string {
    return {
      invalid: 'Revisa que el enlace esté completo.',
      expired: 'La vigencia de este acceso terminó.',
      revoked: 'La persona organizadora desactivó este acceso.',
      replaced: 'Por seguridad, usa el nuevo enlace que te compartieron.',
      suspended: 'La experiencia volverá a estar disponible cuando la organización la reactive.',
      unpublished: 'La organización aún está preparando esta experiencia.',
      loading: 'Estamos preparando tu experiencia.',
      available: '',
    }[this.state()];
  }

  protected stateIcon(): string {
    return this.state() === 'suspended' ? 'Ⅱ' : this.state() === 'replaced' ? '↻' : '!';
  }

  private resolveErrorState(error: unknown): PublicInvitationState {
    if (!(error instanceof HttpErrorResponse)) return 'invalid';
    const body = error.error as { reason?: string } | null;
    const reason = body?.reason;
    return reason === 'expired' ||
      reason === 'revoked' ||
      reason === 'replaced' ||
      reason === 'suspended' ||
      reason === 'unpublished'
      ? reason
      : 'invalid';
  }

  private configurePrivacyMetadata(): void {
    const values: Array<[string, string, string]> = [
      ['name', 'robots', 'noindex, nofollow, noarchive'],
      ['name', 'referrer', 'no-referrer'],
    ];
    for (const [attribute, key, content] of values) {
      let element = this.document.head.querySelector<HTMLMetaElement>(
        `meta[${attribute}="${key}"]`,
      );
      if (!element) {
        element = this.document.createElement('meta');
        element.setAttribute(attribute, key);
        this.document.head.appendChild(element);
      }
      element.content = content;
    }
  }
}

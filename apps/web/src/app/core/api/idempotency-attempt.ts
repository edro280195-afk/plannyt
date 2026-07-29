export type IdempotencyKeyFactory = () => string;

export class IdempotencyAttempt {
  private current: { payload: string; key: string } | null = null;

  constructor(
    private readonly keyFactory: IdempotencyKeyFactory = () =>
      crypto.randomUUID(),
  ) {}

  keyFor(payload: string): string {
    if (!this.current || this.current.payload !== payload) {
      this.current = {
        payload,
        key: this.keyFactory(),
      };
    }

    return this.current.key;
  }

  complete(): void {
    this.current = null;
  }
}

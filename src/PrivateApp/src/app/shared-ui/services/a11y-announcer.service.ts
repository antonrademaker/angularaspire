import { Injectable } from '@angular/core';
import { LiveAnnouncer } from '@angular/cdk/a11y';

@Injectable({
  providedIn: 'root',
})
export class A11yAnnouncerService {
  constructor(private announcer: LiveAnnouncer) {}

  announce(message: string, politeness: 'polite' | 'assertive' = 'polite'): void {
    this.announcer.announce(message, politeness);
  }
}

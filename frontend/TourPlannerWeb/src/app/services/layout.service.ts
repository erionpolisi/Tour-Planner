import { Injectable, signal } from '@angular/core';

/**
 * Coordinates layout chrome state between the navbar and the sidebar.
 * Currently tracks the mobile sidebar drawer (hidden by default,
 * toggled via the hamburger in the navbar, auto-closed when the
 * user picks a nav entry or taps the backdrop).
 */
@Injectable({ providedIn: 'root' })
export class LayoutService {
  readonly sidebarOpen = signal(false);

  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }
}

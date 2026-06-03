import { Injectable, signal, computed } from '@angular/core';

export type SearchScope = 'tours' | 'logs' | null;

@Injectable({
  providedIn: 'root',
})
export class SearchService {
  readonly query = signal('');
  readonly scope = signal<SearchScope>(null);

  /** True whenever a page has set a scope — drives the navbar input's enabled state. */
  readonly active = computed(() => this.scope() !== null);

  /** Context-aware placeholder for the navbar search field. */
  readonly placeholder = computed(() => {
    switch (this.scope()) {
      case 'tours':
        return 'Search tours by name or location…';
      case 'logs':
        return 'Search logs by tour, comment, difficulty…';
      default:
        return 'Search is disabled here';
    }
  });

  /** Set the current page's search scope. Resets the query so stale terms
   *  don't leak from one page into another. */
  setScope(scope: SearchScope): void {
    this.scope.set(scope);
    this.query.set('');
  }

  search(value: string): void {
    this.query.set(value);
  }

  clear(): void {
    this.query.set('');
  }
}


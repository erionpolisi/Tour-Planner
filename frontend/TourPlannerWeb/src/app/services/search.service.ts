import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class SearchService {
  readonly query = signal('');

  search(value: string): void {
    this.query.set(value);
  }
}

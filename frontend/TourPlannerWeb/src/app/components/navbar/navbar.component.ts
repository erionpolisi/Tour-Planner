import { Component, output, signal } from '@angular/core';
import { LucideAngularModule, Map, Search, User } from 'lucide-angular';

@Component({
  selector: 'app-navbar',
  imports: [LucideAngularModule],
  templateUrl: './navbar.component.html',
})
export class NavbarComponent {
  readonly searchChange = output<string>();

  protected readonly icons = { Map, Search, User };
  protected readonly searchValue = signal('');

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchValue.set(value);
    this.searchChange.emit(value);
  }
}

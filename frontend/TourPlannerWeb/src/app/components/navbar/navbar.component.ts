import { Component, inject } from '@angular/core';
import { LucideAngularModule, Map, Search, User } from 'lucide-angular';
import { SearchService } from '../../services/search.service';

@Component({
  selector: 'app-navbar',
  imports: [LucideAngularModule],
  templateUrl: './navbar.component.html',
})
export class NavbarComponent {
  private readonly searchService = inject(SearchService);

  protected readonly icons = { Map, Search, User };

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchService.search(value);
  }
}

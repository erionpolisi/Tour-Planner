import { Component, ElementRef, ViewChild, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule, Map, Search, User, LogOut, X } from 'lucide-angular';
import { SearchService } from '../../services/search.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-navbar',
  imports: [LucideAngularModule, RouterLink],
  templateUrl: './navbar.component.html',
})
export class NavbarComponent {
  protected readonly searchService = inject(SearchService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  @ViewChild('searchInput') private searchInputRef?: ElementRef<HTMLInputElement>;

  protected readonly icons = { Map, Search, User, LogOut, X };
  protected readonly currentUser = this.authService.currentUser;

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchService.search(value);
  }

  onClear(): void {
    this.searchService.clear();
    this.searchInputRef?.nativeElement.focus();
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/auth']);
  }
}

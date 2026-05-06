import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './components/navbar/navbar.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { TourService } from './services/tour.service';
import { TourLogService } from './services/tour-log.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavbarComponent, SidebarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly tourService = inject(TourService);
  private readonly tourLogService = inject(TourLogService);

  onSearch(query: string): void {
    this.tourService.search(query);
    this.tourLogService.search(query);
  }
}

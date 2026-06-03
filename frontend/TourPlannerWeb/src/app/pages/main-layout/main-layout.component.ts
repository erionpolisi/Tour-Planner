import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from '../../components/navbar/navbar.component';
import { SidebarComponent } from '../../components/sidebar/sidebar.component';
import { LayoutService } from '../../services/layout.service';
import { TourDetailModalComponent } from '../../components/tour-detail-modal/tour-detail-modal.component';
import { LogDetailModalComponent } from '../../components/log-detail-modal/log-detail-modal.component';
import { CreateTourModalComponent } from '../../components/create-tour-modal/create-tour-modal.component';
import { AddLogModalComponent } from '../../components/add-log-modal/add-log-modal.component';

/**
 * Layout wrapper for all authenticated pages. Owns the chrome
 * (navbar + sidebar + global modals) and renders the active route
 * via <router-outlet>. The auth page deliberately bypasses this
 * layout so it can be full-screen without sidebar/navbar.
 */
@Component({
  selector: 'app-main-layout',
  imports: [
    RouterOutlet,
    NavbarComponent,
    SidebarComponent,
    TourDetailModalComponent,
    LogDetailModalComponent,
    CreateTourModalComponent,
    AddLogModalComponent,
  ],
  templateUrl: './main-layout.component.html',
})
export class MainLayoutComponent {
  protected readonly layoutService = inject(LayoutService);
}

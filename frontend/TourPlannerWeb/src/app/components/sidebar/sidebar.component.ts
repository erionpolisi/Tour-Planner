import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import {
  LucideAngularModule,
  LucideIconData,
  LayoutDashboard,
  Map,
  ScrollText,
  TrendingUp,
  Star,
} from 'lucide-angular';
import { TourService } from '../../services/tour.service';

@Component({
  selector: 'app-sidebar',
  imports: [LucideAngularModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  protected readonly tourService = inject(TourService);

  protected readonly navItems = [
    { label: 'Dashboard', icon: LayoutDashboard, route: '/dashboard' },
    { label: 'Tours', icon: Map, route: '/tours' },
    { label: 'Logs', icon: ScrollText, route: '/logs' },
  ];

  protected readonly statIcons: Record<string, LucideIconData> = {
    'map': Map,
    'trending-up': TrendingUp,
    'star': Star,
  };
}

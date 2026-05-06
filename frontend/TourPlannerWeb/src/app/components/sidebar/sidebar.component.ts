import { Component, inject, computed } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import {
  LucideAngularModule,
  LucideIconData,
  LayoutDashboard,
  Map,
  ScrollText,
  TrendingUp,
  Star,
  Activity,
} from 'lucide-angular';
import { TourService } from '../../services/tour.service';
import { TourLogService } from '../../services/tour-log.service';

@Component({
  selector: 'app-sidebar',
  imports: [LucideAngularModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  protected readonly tourService = inject(TourService);
  private readonly tourLogService = inject(TourLogService);

  protected readonly navItems = [
    { label: 'Dashboard', icon: LayoutDashboard, route: '/dashboard' },
    { label: 'Tours', icon: Map, route: '/tours' },
    { label: 'Logs', icon: ScrollText, route: '/logs' },
  ];

  protected readonly statIcons: Record<string, LucideIconData> = {
    'map': Map,
    'trending-up': TrendingUp,
    'star': Star,
    'activity': Activity,
  };

  protected readonly allStats = computed(() => {
    const tourStats = this.tourService.stats();
    return [
      ...tourStats,
      { label: 'Avg. Difficulty', value: this.tourLogService.avgDifficulty(), icon: 'activity', color: 'from-violet-500 to-purple-500' },
    ];
  });
}

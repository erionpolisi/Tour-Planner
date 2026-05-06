import { Component, inject } from '@angular/core';
import { TourService } from '../../services/tour.service';
import { TourListComponent } from '../../components/tour-list/tour-list.component';

@Component({
  selector: 'app-tours-page',
  imports: [TourListComponent],
  template: '<app-tour-list></app-tour-list>',
})
export class ToursPageComponent {}

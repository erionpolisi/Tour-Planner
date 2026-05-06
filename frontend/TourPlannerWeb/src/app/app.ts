import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './components/navbar/navbar.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { TourDetailModalComponent } from './components/tour-detail-modal/tour-detail-modal.component';
import { LogDetailModalComponent } from './components/log-detail-modal/log-detail-modal.component';
import { CreateTourModalComponent } from './components/create-tour-modal/create-tour-modal.component';
import { AddLogModalComponent } from './components/add-log-modal/add-log-modal.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavbarComponent, SidebarComponent, TourDetailModalComponent, LogDetailModalComponent, CreateTourModalComponent, AddLogModalComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}

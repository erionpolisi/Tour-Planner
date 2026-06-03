import { Component, inject } from '@angular/core';
import { LucideAngularModule, X, Save, Search, MapPin, Check, AlertCircle } from 'lucide-angular';
import { AddLogViewModel } from '../../viewmodels/add-log.viewmodel';

@Component({
  selector: 'app-add-log-modal',
  imports: [LucideAngularModule],
  host: { style: 'display: contents' },
  templateUrl: './add-log-modal.component.html',
})
export class AddLogModalComponent {
  protected readonly vm = inject(AddLogViewModel);
  protected readonly icons = { X, Save, Search, MapPin, Check, AlertCircle };
}

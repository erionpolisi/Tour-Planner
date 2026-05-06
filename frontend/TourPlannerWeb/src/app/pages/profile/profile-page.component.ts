import { Component, inject } from '@angular/core';
import { LucideAngularModule, User, Mail, Edit2, Save, X } from 'lucide-angular';
import { ProfileViewModel } from '../../viewmodels/profile.viewmodel';

@Component({
  selector: 'app-profile-page',
  imports: [LucideAngularModule],
  providers: [ProfileViewModel],
  host: { class: 'flex-1 min-h-0 overflow-y-auto' },
  templateUrl: './profile-page.component.html',
})
export class ProfilePageComponent {
  protected readonly vm = inject(ProfileViewModel);
  protected readonly icons = { User, Mail, Edit2, Save, X };
}

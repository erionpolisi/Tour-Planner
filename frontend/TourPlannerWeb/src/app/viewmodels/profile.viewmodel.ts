import { Injectable, inject, signal, computed } from '@angular/core';
import { UserService } from '../services/user.service';
import { UserProfile } from '../models/user.model';

@Injectable()
export class ProfileViewModel {
  private readonly userService = inject(UserService);

  readonly profile = this.userService.profile;
  readonly editing = signal(false);
  readonly editForm = signal<UserProfile>({ name: '', email: '' });

  startEdit(): void {
    this.editForm.set({ ...this.profile() });
    this.editing.set(true);
  }

  cancelEdit(): void {
    this.editing.set(false);
  }

  saveEdit(): void {
    this.userService.updateProfile(this.editForm());
    this.editing.set(false);
  }

  updateField(field: keyof UserProfile, value: string): void {
    this.editForm.update((f) => ({ ...f, [field]: value }));
  }
}

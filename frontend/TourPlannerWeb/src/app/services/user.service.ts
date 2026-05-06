import { Injectable, signal } from '@angular/core';
import { UserProfile } from '../models/user.model';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly _profile = signal<UserProfile>({
    name: 'Alex Johnson',
    email: 'alex.johnson@example.com',
  });

  readonly profile = this._profile.asReadonly();

  updateProfile(updated: UserProfile): void {
    this._profile.set(updated);
  }
}

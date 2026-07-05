import { Injectable, PLATFORM_ID, computed, effect, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';

import { UserProfile } from '../models/user.model';
import { AuthService } from './auth.service';

interface UserDto {
  id: string;
  name: string;
  email: string;
  avatar?: string | null;
  createdAt: string;
}

interface UpdateUserDto {
  name: string;
  email: string;
  password?: string;
}

const API_BASE = 'http://localhost:5102/api/auth';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly platformId = inject(PLATFORM_ID);

  private readonly _backendProfile = signal<UserProfile | null>(null);

  readonly profile = computed<UserProfile>(() => {
    const backend = this._backendProfile();
    if (backend) return backend;

    const currentUser = this.auth.currentUser();
    if (currentUser) {
      return {
        name: currentUser.name,
        email: currentUser.email,
        avatar: currentUser.avatar ?? undefined,
      };
    }

    return {
      name: '',
      email: '',
    };
  });

  constructor() {
    effect(() => {
      const currentUser = this.auth.currentUser();

      if (!currentUser) {
        this._backendProfile.set(null);
        return;
      }

      if (!isPlatformBrowser(this.platformId)) return;

      this.reloadProfile();
    });
  }

  reloadProfile(): void {
    if (!this.auth.isAuthenticated()) {
      this._backendProfile.set(null);
      return;
    }

    this.http.get<UserDto>(`${API_BASE}/me`).subscribe({
      next: dto => this._backendProfile.set(this.fromDto(dto)),
      error: () => this._backendProfile.set(null),
    });
  }

  updateProfile(updated: UserProfile, password?: string): void {
  const body: UpdateUserDto = {
    name: updated.name.trim(),
    email: updated.email.trim(),
  };

  if (password?.trim()) {
    body.password = password.trim();
  }

  this.http.put<UserDto>(`${API_BASE}/me`, body).subscribe({
    next: dto => {
      this._backendProfile.set(this.fromDto(dto));
    },
    error: err => {
      console.error('Failed to update profile', err);
    },
  });
}

  private fromDto(dto: UserDto): UserProfile {
    return {
      name: dto.name,
      email: dto.email,
      avatar: dto.avatar ?? undefined,
    };
  }
}
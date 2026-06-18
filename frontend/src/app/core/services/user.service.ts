import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

import {
  User,
  SyncUserDto,
  UpdateUserDto,
  MeUserDto
} from '../models/user.model';

export interface UserSearchResult {
  userId: string;
  displayName: string;
  avatarUrl?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class UserService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getAll(): Observable<User[]> {
    return this.get<User[]>(
      API_ENDPOINTS.users
    );
  }

  getById(userId: string): Observable<User> {
    return this.get<User>(
      `${API_ENDPOINTS.users}/${userId}`
    );
  }

  sync(dto: SyncUserDto): Observable<User> {
    return this.post<User>(
      `${API_ENDPOINTS.users}/sync`,
      dto
    );
  }

  update(userId: string, dto: UpdateUserDto): Observable<User> {
    return this.put<User>(
      `${API_ENDPOINTS.users}/${userId}`,
      dto
    );
  }

  getMe(): Observable<MeUserDto> {
    return this.get<MeUserDto>(
      `${API_ENDPOINTS.users}/me`
    );
  }

  searchUsers(term: string): Observable<UserSearchResult[]> {
    return this.get<UserSearchResult[]>(
      `${API_ENDPOINTS.users}/search?term=${encodeURIComponent(term)}`
    );
  }
}
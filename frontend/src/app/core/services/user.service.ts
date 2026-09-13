import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';

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
  private readonly currentUserSubject = new BehaviorSubject<MeUserDto | null>(null);

  readonly currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  get currentUser(): MeUserDto | null {
    return this.currentUserSubject.value;
  }

  get isAdmin(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'admin';
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
    ).pipe(
      tap(me => this.currentUserSubject.next(me))
    );
  }

  searchUsers(term: string): Observable<UserSearchResult[]> {
    return this.get<UserSearchResult[]>(
      `${API_ENDPOINTS.users}/search?term=${encodeURIComponent(term)}`
    );
  }
}

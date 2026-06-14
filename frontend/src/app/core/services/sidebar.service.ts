import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import { SidebarData } from '../models/sidebar.model.js';

@Injectable({
  providedIn: 'root'
})
export class SidebarService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getSidebarData(): Observable<SidebarData> {
    return this.get<SidebarData>(
          `${API_ENDPOINTS.sidebar}`
        );
  }
}
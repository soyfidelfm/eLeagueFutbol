import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';


import { ApiService } from '../services/api.service.ts';
import { API_ENDPOINTS } from '../constants/api-endpoints';

export interface CloudinaryUploadResponse {
  secureUrl: string;
  publicId: string;
}

@Injectable({
  providedIn: 'root'
})
export class CloudinaryService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  uploadImage(file: File): Observable<CloudinaryUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.post<CloudinaryUploadResponse>(
      `${API_ENDPOINTS.cloudinary}/upload`,
      formData
    );
  }
  uploadAvatar(file: File): Observable<CloudinaryUploadResponse> {
  const formData = new FormData();

  formData.append(
    'file',
    file
  );

  return this.post<CloudinaryUploadResponse>(
    `${API_ENDPOINTS.cloudinary}/upload-avatar`,
    formData
  );
}
}
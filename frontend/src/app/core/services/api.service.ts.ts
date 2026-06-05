import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(
    protected http: HttpClient
  ) { }

  protected get<T>(
    url: string,
    params?: HttpParams
  ): Observable<T> {

    return this.http.get<T>(
      url,
      { params }
    );
  }

  protected post<T>(
    url: string,
    body: unknown
  ): Observable<T> {

    return this.http.post<T>(
      url,
      body
    );
  }

  protected put<T>(
    url: string,
    body: unknown
  ): Observable<T> {

    return this.http.put<T>(
      url,
      body
    );
  }

  protected patch<T>(
    url: string,
    body: unknown
  ): Observable<T> {

    return this.http.patch<T>(
      url,
      body
    );
  }

  protected delete<T>(
    url: string
  ): Observable<T> {

    return this.http.delete<T>(
      url
    );
  }
}
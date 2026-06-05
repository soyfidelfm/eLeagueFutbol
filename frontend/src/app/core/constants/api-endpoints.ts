import { environment } from '../../../environments/environment';

export const API_ENDPOINTS = {

  seasons:
    `${environment.api.baseUrl}/api/seasons`,

  teams:
    `${environment.api.baseUrl}/api/teams`,

  tournaments:
    `${environment.api.baseUrl}/api/tournaments`,

  matches:
    `${environment.api.baseUrl}/api/matches`,

  users:
    `${environment.api.baseUrl}/api/users`
};
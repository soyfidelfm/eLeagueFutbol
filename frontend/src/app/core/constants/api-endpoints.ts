import { environment } from '../../../environments/environment';

export const API_ENDPOINTS = {
  seasons: `${environment.api.baseUrl}/api/seasons`,
  teams: `${environment.api.baseUrl}/api/teams`,
  tournaments: `${environment.api.baseUrl}/api/tournaments`,
  matches: `${environment.api.baseUrl}/api/matches`,
  users: `${environment.api.baseUrl}/api/users`,
  dashboard: `${environment.api.baseUrl}/api/dashboard`,
  gameClubs: `${environment.api.baseUrl}/api/game-clubs`,
  statsOverview: `${environment.api.baseUrl}/api/stats/overview`,
  // api-endpoints.ts
matchStatsOcrPreview: `${environment.api.baseUrl}/api/match-stats/ocr-preview`,
matchStatsConfirmPlayer: `${environment.api.baseUrl}/api/match-stats/confirm-player`,
clubs: `${environment.api.baseUrl}/api/clubs`,
cloudinary: `${environment.api.baseUrl}/api/cloudinary`
};
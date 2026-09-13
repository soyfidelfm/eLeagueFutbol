import { environment } from '../../../environments/environment';

export const API_ENDPOINTS = {
  seasons: `${environment.api.baseUrl}/api/seasons`,
  tournaments: `${environment.api.baseUrl}/api/tournaments`,
  matches: `${environment.api.baseUrl}/api/matches`,
  users: `${environment.api.baseUrl}/api/users`,
  dashboard: `${environment.api.baseUrl}/api/dashboard`,
  gameData: `${environment.api.baseUrl}/api/game-data`,
  statsOverview: `${environment.api.baseUrl}/api/stats/overview`,

  // MATCH STATS
  matchStatsOcrSummaryPreview:
    `${environment.api.baseUrl}/api/match-stats/ocr-summary-preview`,

  matchStatsOcrTeamPerformancePreview:
    `${environment.api.baseUrl}/api/match-stats/ocr-team-performance-preview`,

  matchStatsConfirmSummary:
    `${environment.api.baseUrl}/api/match-stats/confirm-summary`,

  matchStatsConfirmTeamPerformance:
    `${environment.api.baseUrl}/api/match-stats/confirm-team-performance`,

  // NEW FLOW (3 screenshots)
  matchStatsUpload: (matchId: string) =>
    `${environment.api.baseUrl}/api/matches/${matchId}/stats-upload`,

  clubs: `${environment.api.baseUrl}/api/clubs`,
  cloudinary: `${environment.api.baseUrl}/api/cloudinary`,
  rankings: `${environment.api.baseUrl}/api/rankings`,
  sidebar: `${environment.api.baseUrl}/api/sidebar`,
  draft: `${environment.api.baseUrl}/api/draft`,
  notifications: `${environment.api.baseUrl}/api/notifications`,
  messages: `${environment.api.baseUrl}/api/messages`,
  transfers: `${environment.api.baseUrl}/api/transfers`
};

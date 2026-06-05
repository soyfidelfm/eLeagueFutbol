import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiService } from '../services/api.service.ts';

import { API_ENDPOINTS } from '../constants/api-endpoints';

import { Activity } from '../models/activity.model';
import { FeaturedPlayer } from '../models/featuredplayer.model';
import { Kpi } from '../models/kpi.model';
import { Match } from '../models/match.model';
import { MatchOfWeek } from '../models/matchofweek.model';
import { MyStats } from '../models/mystats.model';
import { NewsItem } from '../models/newsitem.model';
import { Player } from '../models/player.model';
import { UpcomingMatch } from '../models/upcomingmatch.model';

export interface DashboardResponse {
  kpis: Kpi[];
  leaderboard: Player[];
  matchOfWeek: MatchOfWeek | null;
  matches: Match[];
  upcomingMatches: UpcomingMatch[];
  news: NewsItem[];
  playerOfWeek: FeaturedPlayer | null;
  myStats: MyStats;
  activities: Activity[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService extends ApiService {

  constructor(
    protected override http: HttpClient
  ) {
    super(http);
  }

  getDashboard(): Observable<DashboardResponse> {

    const timeZone =
      Intl.DateTimeFormat()
        .resolvedOptions()
        .timeZone;

    const params = new HttpParams()
      .set(
        'timeZone',
        timeZone
      );

    return this.get<DashboardResponse>(
      API_ENDPOINTS.dashboard,
      params
    );
  }
}
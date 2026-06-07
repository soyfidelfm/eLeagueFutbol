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
  currentSeasonId?: string | null;
  currentSeasonName?: string | null;

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
import { FeaturedPlayer } from "./featuredplayer.model";

export interface MatchOfWeek {
  arena: string;
  stage: string;
  date: string;
  countdown: string;
  home: FeaturedPlayer;
  away: FeaturedPlayer;
}
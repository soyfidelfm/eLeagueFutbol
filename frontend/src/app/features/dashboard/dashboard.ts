import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

import { Activity } from '../../core/models/activity.model';
import { FeaturedPlayer } from '../../core/models/featuredplayer.model';
import { Kpi } from '../../core/models/kpi.model';
import { Match } from '../../core/models/match.model';
import { MatchOfWeek } from '../../core/models/matchofweek.model';
import { MyStats } from '../../core/models/mystats.model';
import { NewsItem } from '../../core/models/newsitem.model';
import { Player } from '../../core/models/player.model';
import { SeasonProgress } from '../../core/models/seasonprogress.model';
import { UpcomingMatch } from '../../core/models/upcomingmatch.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
  leaderboard: Player[] = [];

  matches: Match[] = [];

  activities: Activity[] = [];

  kpis: Kpi[] = [
    { label: 'Season', value: 'Season 7', sub: 'Champions Arena' },
    { label: 'Players', value: '2,438', sub: 'Registered' },
    { label: 'Teams', value: '312', sub: 'Active Clubs' },
    { label: 'Live Now', value: '18', sub: 'Matches' },
    { label: 'This Week', value: '145', sub: 'Matches Played' }
  ];

  matchOfWeek: MatchOfWeek = {
    arena: 'Champions Arena',
    stage: 'Quarter Finals',
    date: 'Sunday, 8:00 PM PST',
    countdown: 'Starts in 04:32',
    home: {
      name: 'EliteStriker',
      rank: 1,
      rating: 91,
      image: '/images/player1.png',
      streak: '4 Wins Streak'
    },
    away: {
      name: 'RazorX',
      rank: 2,
      rating: 89,
      image: '/images/player2.png',
      streak: '5 Wins Streak'
    }
  };

  upcomingMatches: UpcomingMatch[] = [
    {
      home: 'EliteStriker',
      away: 'RazorX',
      date: 'Sun, 8:00 PM',
      stage: 'Quarter Finals'
    },
    {
      home: 'NeoFut',
      away: 'Dribbl3God',
      date: 'Mon, 9:00 PM',
      stage: 'Quarter Finals'
    },
    {
      home: 'CyberMessi',
      away: 'Phantom7',
      date: 'Tue, 8:30 PM',
      stage: 'Round of 16'
    }
  ];

  news: NewsItem[] = [
    { title: 'Season 7 starts this Friday!', time: '2h ago' },
    { title: 'Transfer Window is now open', time: '5h ago' },
    { title: 'Patch Notes v1.2 is live', time: '1d ago' },
    { title: 'Global Cup registration opened', time: '2d ago' }
  ];

  playerOfWeek: FeaturedPlayer = {
    name: 'EliteStriker',
    rank: 1,
    rating: 91,
    image: '/images/player1.png',
    streak: '87 Goals · 46 Assists · 72% Win Rate'
  };

  myStats: MyStats = {
    winRate: '72%',
    goals: 87,
    assists: 46,
    cleanSheets: 18,
    avgPossession: '58%'
  };

  season: SeasonProgress = {
    rank: 'ELITE I',
    points: 2450,
    nextRank: 2700,
    percent: 72
  };

  constructor() {
    this.loadMockData();
  }

  private loadMockData(): void {
    this.leaderboard = [
      {
        rank: 1,
        name: 'EliteStriker',
        points: 2450,
        gd: '+45',
        record: '28/6',
        avatar: '/images/avatar1.png',
        form: ['W', 'W', 'W', 'W', 'L']
      },
      {
        rank: 2,
        name: 'RazorX',
        points: 2310,
        gd: '+33',
        record: '26/7',
        avatar: '/images/avatar2.png',
        form: ['W', 'W', 'L', 'W', 'W']
      },
      {
        rank: 3,
        name: 'NeoFut',
        points: 2180,
        gd: '+29',
        record: '24/8',
        avatar: '/images/avatar3.png',
        form: ['W', 'L', 'W', 'W', 'W']
      },
      {
        rank: 4,
        name: 'Dribbl3God',
        points: 2120,
        gd: '+22',
        record: '22/9',
        avatar: '/images/avatar4.png',
        form: ['W', 'W', 'W', 'L', 'W']
      },
      {
        rank: 5,
        name: 'CyberMessi',
        points: 2045,
        gd: '+18',
        record: '21/9',
        avatar: '/images/avatar5.png',
        form: ['W', 'L', 'W', 'W', 'W']
      }
    ];

    this.matches = [
      {
        stage: 'Semi Finals',
        score: '2 - 1',
        status: 'LIVE'
      },
      {
        stage: 'Round of 16',
        score: '0 - 0',
        status: '30 MIN'
      },
      {
        stage: 'Round of 16',
        score: '1 - 0',
        status: 'UPCOMING'
      }
    ];

    this.activities = [
      {
        name: 'EliteStriker',
        text: 'defeated NovaPlayer 4-1',
        time: '2m ago',
        avatar: '/images/avatar1.png'
      },
      {
        name: 'RazorX',
        text: 'checked in for match',
        time: '4m ago',
        avatar: '/images/avatar2.png'
      },
      {
        name: 'TitanPro',
        text: 'reached Elite Rank',
        time: '7m ago',
        avatar: '/images/avatar3.png'
      },
      {
        name: 'GhostFC',
        text: 'won 3-2 against Yoshi',
        time: '12m ago',
        avatar: '/images/avatar4.png'
      }
    ];
  }
}
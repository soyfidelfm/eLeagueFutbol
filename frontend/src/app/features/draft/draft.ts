import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface DraftTeam {
  pick: number;
  name: string;
  logo: string;
  isCurrent?: boolean;
}

interface DraftPlayer {
  id: number;
  name: string;
  rating: number;
  position: string;
  club: string;
  country: string;
  imageUrl: string;
  pac: number;
  sho: number;
  pas: number;
  dri: number;
  def: number;
  phy: number;
}

interface DraftPick {
  pick: number;
  team: string;
  player: string;
  rating: number;
  imageUrl: string;
}

interface SquadSlot {
  position: string;
  player?: DraftPlayer;
}

@Component({
  selector: 'app-draft',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './draft.html',
  styleUrl: './draft.scss'
})
export class Draft implements OnInit, OnDestroy {
  isLoading = true;

  searchTerm = '';
  selectedPosition = 'All';
  currentPage = 1;
  pageSize = 9;

  secondsRemaining = 151;
  timerText = '00:02:31';

  private timerId?: number;
  private loadingTimeoutId?: number;

  positions = ['All', 'ST', 'LW', 'RW', 'CM', 'CDM', 'CB', 'LB', 'RB', 'GK'];

  draftOrder: DraftTeam[] = [
    { pick: 1, name: 'Titans FC', logo: '🟣' },
    { pick: 2, name: 'Legends FC', logo: '🔵' },
    { pick: 3, name: 'Galaxy United', logo: '🟡' },
    { pick: 4, name: 'Kings FC', logo: '⚫' },
    { pick: 5, name: 'Elite FC', logo: '🔴' },
    { pick: 6, name: 'Warriors United', logo: '⚪' },
    { pick: 7, name: 'Black Panthers', logo: '⚫' },
    { pick: 8, name: 'Red Devils', logo: '🔴' },
    { pick: 9, name: 'Royal Club', logo: '⚪' },
    { pick: 10, name: 'Storm FC', logo: '⚫' },
    { pick: 11, name: 'Venom Team', logo: '⚪' },
    { pick: 12, name: 'E-League FC', logo: '🟢', isCurrent: true }
  ];

  players: DraftPlayer[] = [
    {
      id: 1,
      name: 'K. Mbappé',
      rating: 92,
      position: 'ST',
      club: 'Real Madrid',
      country: 'France',
      imageUrl: '/images/players/player-placeholder.png',
      pac: 98,
      sho: 91,
      pas: 83,
      dri: 93,
      def: 37,
      phy: 79
    },
    {
      id: 2,
      name: 'Rodri',
      rating: 91,
      position: 'CDM',
      club: 'Manchester City',
      country: 'Spain',
      imageUrl: '/images/players/player-placeholder.png',
      pac: 75,
      sho: 80,
      pas: 89,
      dri: 84,
      def: 87,
      phy: 85
    },
    {
      id: 3,
      name: 'Vini Jr.',
      rating: 90,
      position: 'LW',
      club: 'Real Madrid',
      country: 'Brazil',
      imageUrl: '/images/players/player-placeholder.png',
      pac: 96,
      sho: 84,
      pas: 81,
      dri: 91,
      def: 29,
      phy: 69
    },
    {
      id: 4,
      name: 'Mohamed Salah',
      rating: 90,
      position: 'RW',
      club: 'Liverpool',
      country: 'Egypt',
      imageUrl: '/images/players/player-placeholder.png',
      pac: 90,
      sho: 89,
      pas: 82,
      dri: 90,
      def: 45,
      phy: 75
    },
    {
      id: 5,
      name: 'Bellingham',
      rating: 89,
      position: 'CM',
      club: 'Real Madrid',
      country: 'England',
      imageUrl: '/images/players/player-placeholder.png',
      pac: 79,
      sho: 82,
      pas: 85,
      dri: 90,
      def: 78,
      phy: 82
    },
    {
      id: 6,
      name: 'Haaland',
      rating: 89,
      position: 'ST',
      club: 'Manchester City',
      country: 'Norway',
      imageUrl: '/images/players/player-placeholder.png',
      pac: 90,
      sho: 92,
      pas: 67,
      dri: 81,
      def: 45,
      phy: 88
    },
    {
      id: 7,
      name: 'Rúben Dias',
      rating: 88,
      position: 'CB',
      club: 'Manchester City',
      country: 'Portugal',
      imageUrl: '/images/players/player-placeholder.png',
      pac: 67,
      sho: 39,
      pas: 70,
      dri: 69,
      def: 89,
      phy: 87
    },
    {
      id: 8,
      name: 'Alisson',
      rating: 88,
      position: 'GK',
      club: 'Liverpool',
      country: 'Brazil',
      imageUrl: '/images/players/player-placeholder.png',
      pac: 86,
      sho: 85,
      pas: 85,
      dri: 89,
      def: 56,
      phy: 89
    },
    {
      id: 9,
      name: 'Pedri',
      rating: 87,
      position: 'CM',
      club: 'Barcelona',
      country: 'Spain',
      imageUrl: '/images/players/player-placeholder.png',
      pac: 78,
      sho: 69,
      pas: 87,
      dri: 89,
      def: 70,
      phy: 74
    }
  ];

  squadSlots: SquadSlot[] = [
    { position: 'ST' },
    { position: 'LW' },
    { position: 'RW' },
    { position: 'CM' },
    { position: 'CM' },
    { position: 'CDM' },
    { position: 'LB' },
    { position: 'CB' },
    { position: 'CB' },
    { position: 'RB' },
    { position: 'GK' }
  ];

  lastPicks: DraftPick[] = [
    {
      pick: 11,
      team: 'Black Panthers',
      player: 'F. Valverde',
      rating: 88,
      imageUrl: '/images/players/player-placeholder.png'
    },
    {
      pick: 10,
      team: 'Storm FC',
      player: 'T. Alexander-Arnold',
      rating: 87,
      imageUrl: '/images/players/player-placeholder.png'
    },
    {
      pick: 9,
      team: 'Royal Club',
      player: 'K. De Bruyne',
      rating: 87,
      imageUrl: '/images/players/player-placeholder.png'
    },
    {
      pick: 8,
      team: 'Red Devils',
      player: 'A. Griezmann',
      rating: 86,
      imageUrl: '/images/players/player-placeholder.png'
    }
  ];

  ngOnInit(): void {
    this.loadDraft();
  }

  ngOnDestroy(): void {
    if (this.timerId) {
      window.clearInterval(this.timerId);
    }

    if (this.loadingTimeoutId) {
      window.clearTimeout(this.loadingTimeoutId);
    }
  }

  get draftedCount(): number {
    return this.squadSlots.filter(slot => slot.player).length;
  }

  get filteredPlayers(): DraftPlayer[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.players.filter(player => {
      const matchesPosition =
        this.selectedPosition === 'All' ||
        player.position === this.selectedPosition;

      const matchesSearch =
        !term ||
        player.name.toLowerCase().includes(term) ||
        player.club.toLowerCase().includes(term) ||
        player.country.toLowerCase().includes(term);

      return matchesPosition && matchesSearch;
    });
  }

  get pagedPlayers(): DraftPlayer[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredPlayers.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredPlayers.length / this.pageSize));
  }

  loadDraft(): void {
    this.isLoading = true;

    this.loadingTimeoutId = window.setTimeout(() => {
      this.isLoading = false;
      this.startTimer();
    }, 800);
  }

  setPosition(position: string): void {
    this.selectedPosition = position;
    this.currentPage = 1;
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  draftPlayer(player: DraftPlayer): void {
    const emptySlot = this.squadSlots.find(slot => !slot.player);

    if (!emptySlot) {
      return;
    }

    emptySlot.player = player;
    this.players = this.players.filter(x => x.id !== player.id);

    if (this.currentPage > this.totalPages) {
      this.currentPage = this.totalPages;
    }
  }

  private startTimer(): void {
    if (this.timerId) {
      window.clearInterval(this.timerId);
    }

    this.timerText = this.formatTime(this.secondsRemaining);

    this.timerId = window.setInterval(() => {
      if (this.secondsRemaining <= 0) {
        window.clearInterval(this.timerId);
        this.timerText = '00:00:00';
        return;
      }

      this.secondsRemaining--;
      this.timerText = this.formatTime(this.secondsRemaining);
    }, 1000);
  }

  private formatTime(totalSeconds: number): string {
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `00:${minutes.toString().padStart(2, '0')}:${seconds
      .toString()
      .padStart(2, '0')}`;
  }
}
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter, Subscription, timer } from 'rxjs';

import { SidebarService } from '../../core/services/sidebar.service';
import { SidebarData } from '../../core/models/sidebar.model';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule
  ],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss'
})
export class Sidebar implements OnInit, OnDestroy {

  currentSection = 'dashboard';
  seasonView: 'current' | 'list' | 'create' = 'list';

  sidebarData: SidebarData | null = null;

  days = '00';
  hours = '00';
  minutes = '00';

  private subscriptions = new Subscription();
  private countdownStarted = false;

  constructor(
    private router: Router,
    private sidebarService: SidebarService,
    private cdr: ChangeDetectorRef
  ) {
    this.setCurrentSection(this.router.url);

    this.subscriptions.add(
      this.router.events
        .pipe(filter(event => event instanceof NavigationEnd))
        .subscribe(event => {
          const navEnd = event as NavigationEnd;
          this.setCurrentSection(navEnd.urlAfterRedirects);
        })
    );
  }

  ngOnInit(): void {
    this.loadSidebarData();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadSidebarData(): void {
    this.sidebarService.getSidebarData()
      .subscribe({
        next: response => {
          console.log('SIDEBAR DATA:', response);

          this.sidebarData = response;

          this.updateCountdown();

          if (!this.countdownStarted) {
            this.startCountdown();
            this.countdownStarted = true;
          }

          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Error loading sidebar data', error);
        }
      });
  }

  private startCountdown(): void {
    this.subscriptions.add(
      timer(60000, 60000).subscribe(() => {
        this.updateCountdown();
        this.cdr.detectChanges();
      })
    );
  }

  private updateCountdown(): void {
    const startsAtUtc = this.sidebarData?.cup?.startsAtUtc;

    if (!startsAtUtc) {
      this.days = '00';
      this.hours = '00';
      this.minutes = '00';
      return;
    }

    const startTime = new Date(startsAtUtc).getTime();
    const now = new Date().getTime();

    const difference = startTime - now;

    if (difference <= 0) {
      this.days = '00';
      this.hours = '00';
      this.minutes = '00';
      return;
    }

    const totalMinutes = Math.floor(difference / 1000 / 60);

    const days = Math.floor(totalMinutes / 1440);
    const hours = Math.floor((totalMinutes % 1440) / 60);
    const minutes = totalMinutes % 60;

    this.days = days.toString().padStart(2, '0');
    this.hours = hours.toString().padStart(2, '0');
    this.minutes = minutes.toString().padStart(2, '0');
  }

  private setCurrentSection(url: string): void {
    const cleanUrl = url.split('?')[0];

    if (url.includes('view=create')) {
      this.seasonView = 'create';
    }
    else if (url.includes('view=current')) {
      this.seasonView = 'current';
    }
    else {
      this.seasonView = 'list';
    }

    if (cleanUrl.startsWith('/seasons')) {
      this.currentSection = 'seasons';
      return;
    }

    if (cleanUrl.startsWith('/arena')) {
      this.currentSection = 'arena';
      return;
    }

    if (cleanUrl.startsWith('/clubs')) {
      this.currentSection = 'clubs';
      return;
    }

    if (cleanUrl.startsWith('/rankings')) {
      this.currentSection = 'rankings';
      return;
    }

    if (cleanUrl.startsWith('/stats')) {
      this.currentSection = 'stats';
      return;
    }

    this.currentSection = 'dashboard';
  }
}
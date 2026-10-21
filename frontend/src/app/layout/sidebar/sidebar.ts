import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter, Subscription } from 'rxjs';

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

  private subscriptions = new Subscription();

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
          this.sidebarData = response;
          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Error loading sidebar data', error);
        }
      });
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

    if (cleanUrl.startsWith('/messages')) {
      this.currentSection = 'messages';
      return;
    }

    if (cleanUrl.startsWith('/draft')) {
      this.currentSection = 'draft';
      return;
    }

    this.currentSection = 'dashboard';
  }
}

import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';

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
export class Sidebar {

  currentSection = 'dashboard';
  seasonView: 'current' | 'list' | 'create' = 'list';

  constructor(private router: Router) {
    this.setCurrentSection(this.router.url);

    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(event => {
        const navEnd = event as NavigationEnd;
        this.setCurrentSection(navEnd.urlAfterRedirects);
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
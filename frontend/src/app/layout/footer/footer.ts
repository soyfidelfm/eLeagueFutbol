import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { take } from 'rxjs';

import { SeasonService } from '../../core/services/season.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [
    RouterModule
  ],
  templateUrl: './footer.html',
  styleUrl: './footer.scss'
})
export class Footer {

  constructor(
    private seasonService: SeasonService,
    private router: Router
  ) {}

  openDraft(): void {
    this.seasonService.getAll()
      .pipe(take(1))
      .subscribe({
        next: seasons => {
          const currentSeason = (seasons ?? []).find(
            x => x.isCurrent && x.isActive
          );

          if (!currentSeason?.seasonId) {
            return;
          }

          this.router.navigate(['/draft'], {
            queryParams: {
              seasonId: currentSeason.seasonId
            }
          });
        },
        error: err => {
          console.error('Error loading current season', err);
        }
      });
  }
}
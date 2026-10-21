import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule,
    TranslatePipe
  ],
  templateUrl: './app-loading.html',
  styleUrl: './app-loading.scss'
})
export class AppLoading {
  @Input() title = 'Loading';
  @Input() message = 'Please wait...';
  @Input() logo = true;
  @Input() small = false;
  @Input() showBar = true;
  @Input() extraClass = '';
}
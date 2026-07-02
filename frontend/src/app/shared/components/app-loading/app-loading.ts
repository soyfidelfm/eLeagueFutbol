import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule],
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
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { AuthService } from './core/auth/auth.service';
import { BasketService } from './core/services/basket.service';

@Component({
  selector: 'tedu-root',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatToolbarModule, MatButtonModule, MatIconModule, MatBadgeModule,
  ],
  template: `
    <div class="app-container">
      <mat-toolbar color="primary" class="app-toolbar">
        <a routerLink="/" class="text-xl font-semibold mr-6">Tedu Shop</a>
        <a mat-button routerLink="/products" routerLinkActive="bg-brand-700">Products</a>
        @if (auth.isAuthenticated()) {
          <a mat-button routerLink="/basket" routerLinkActive="bg-brand-700">
            <mat-icon [matBadge]="basket.itemCount() || null" matBadgeColor="warn">shopping_cart</mat-icon>
            Cart
          </a>
          <a mat-button routerLink="/orders" routerLinkActive="bg-brand-700">Orders</a>
        }
        <span class="flex-1"></span>
        @if (auth.isAuthenticated()) {
          <span class="text-sm mr-3">{{ auth.identity()?.email }}</span>
          <button mat-stroked-button color="accent" (click)="auth.logout()">Sign out</button>
        } @else {
          <button mat-flat-button color="accent" (click)="auth.login()">Sign in</button>
        }
      </mat-toolbar>
      <main class="app-content"><router-outlet /></main>
    </div>
  `,
})
export class AppComponent {
  readonly auth = inject(AuthService);
  readonly basket = inject(BasketService);
}

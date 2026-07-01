import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  {
    path: 'products',
    loadComponent: () =>
      import('./features/product/product-list.component').then((m) => m.ProductListComponent),
  },
  {
    path: 'products/:id',
    loadComponent: () =>
      import('./features/product/product-detail.component').then((m) => m.ProductDetailComponent),
  },
  {
    path: 'basket',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/basket/basket.component').then((m) => m.BasketComponent),
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/order/order-list.component').then((m) => m.OrderListComponent),
  },
  {
    path: 'orders/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/order/order-detail.component').then((m) => m.OrderDetailComponent),
  },
  {
    path: 'auth-callback',
    loadComponent: () =>
      import('./features/auth/auth-callback.component').then((m) => m.AuthCallbackComponent),
  },
  {
    path: 'silent-renew',
    loadComponent: () =>
      import('./features/auth/silent-renew.component').then((m) => m.SilentRenewComponent),
  },
  { path: '**', redirectTo: 'products' },
];

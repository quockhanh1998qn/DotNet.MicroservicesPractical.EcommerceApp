import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';
import { environment } from '@env';
import { BasketCheckout, Cart, CartItem } from '../models/basket.model';

@Injectable({ providedIn: 'root' })
export class BasketService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBase}/baskets`;

  private readonly _cart = signal<Cart | null>(null);
  readonly cart = this._cart.asReadonly();
  readonly itemCount = computed(() =>
    (this._cart()?.items ?? []).reduce((sum: number, item: CartItem) => sum + item.quantity, 0),
  );

  load(username: string): Observable<Cart> {
    return this.http.get<Cart>(`${this.base}/${username}`).pipe(
      tap((cart) => this._cart.set(cart)),
      catchError((err: HttpErrorResponse) => {
        if (err.status === 404) {
          const empty: Cart = { username, items: [] };
          this._cart.set(empty);
          return of(empty);
        }
        throw err;
      }),
    );
  }

  upsert(cart: Cart): Observable<Cart> {
    return this.http.post<Cart>(this.base, cart).pipe(tap((updated) => this._cart.set(updated)));
  }

  remove(username: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${username}`).pipe(tap(() => this._cart.set(null)));
  }

  checkout(payload: BasketCheckout): Observable<void> {
    return this.http.post<void>(`${this.base}/checkout`, payload);
  }
}

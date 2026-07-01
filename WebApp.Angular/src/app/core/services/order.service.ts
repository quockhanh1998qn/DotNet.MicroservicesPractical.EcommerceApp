import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env';
import { Order } from '../models/order.model';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBase}/orders`;

  listByUser(username: string): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.base}/${username}`);
  }

  getById(id: number): Observable<Order> {
    return this.http.get<Order>(`${this.base}/id/${id}`);
  }
}

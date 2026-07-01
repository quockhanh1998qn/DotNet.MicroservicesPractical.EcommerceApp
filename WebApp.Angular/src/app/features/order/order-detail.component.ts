import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { OrderService } from '@core/services/order.service';
import { Order } from '@core/models/order.model';

@Component({
  selector: 'tedu-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule],
  template: `
    <a routerLink="/orders" class="btn-secondary inline-block mb-3">&larr; Back to orders</a>
    @if (order(); as o) {
      <mat-card class="card max-w-2xl">
        <h1 class="text-2xl font-semibold mb-2">Order #{{ o.id }}</h1>
        <p>Status: <strong>{{ o.status }}</strong></p>
        <p>Created: {{ o.createdDate | date:'medium' }}</p>
        <p>Total: <strong>\${{ o.totalPrice | number:'1.2-2' }}</strong></p>
        <hr class="my-3">
        <p>Shipping: {{ o.shippingAddress }}</p>
        <p>Invoice: {{ o.invoiceAddress }}</p>
      </mat-card>
    } @else {
      <p>Loading order...</p>
    }
  `,
})
export class OrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(OrderService);

  readonly order = signal<Order | null>(null);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getById(id).subscribe((o) => this.order.set(o));
  }
}

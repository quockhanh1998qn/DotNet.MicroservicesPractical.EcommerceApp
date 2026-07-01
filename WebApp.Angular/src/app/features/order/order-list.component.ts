import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { OrderService } from '@core/services/order.service';
import { AuthService } from '@core/auth/auth.service';
import { Order } from '@core/models/order.model';

@Component({
  selector: 'tedu-order-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatTableModule],
  template: `
    <h1 class="text-2xl font-semibold mb-4">Your orders</h1>

    @if (orders().length === 0) {
      <p>No orders yet.</p>
    } @else {
      <table mat-table [dataSource]="orders()" class="card w-full">
        <ng-container matColumnDef="id">
          <th mat-header-cell *matHeaderCellDef>Order #</th>
          <td mat-cell *matCellDef="let o"><a [routerLink]="['/orders', o.id]" class="text-brand-600 hover:underline">{{ o.id }}</a></td>
        </ng-container>
        <ng-container matColumnDef="date">
          <th mat-header-cell *matHeaderCellDef>Date</th>
          <td mat-cell *matCellDef="let o">{{ o.createdDate | date:'short' }}</td>
        </ng-container>
        <ng-container matColumnDef="total">
          <th mat-header-cell *matHeaderCellDef>Total</th>
          <td mat-cell *matCellDef="let o">\${{ o.totalPrice | number:'1.2-2' }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let o">{{ o.status }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols;"></tr>
      </table>
    }
  `,
})
export class OrderListComponent implements OnInit {
  private readonly api = inject(OrderService);
  private readonly auth = inject(AuthService);

  readonly orders = signal<Order[]>([]);
  readonly cols = ['id', 'date', 'total', 'status'];

  ngOnInit(): void {
    const username = this.auth.identity()?.email;
    if (!username) return;
    this.api.listByUser(username).subscribe((o) => this.orders.set(o));
  }
}

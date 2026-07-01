import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { ProductService } from '@core/services/product.service';
import { Product } from '@core/models/product.model';

@Component({
  selector: 'tedu-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatCardModule, MatPaginatorModule],
  template: `
    <div class="flex items-center gap-3 mb-4">
      <mat-form-field appearance="outline" class="flex-1">
        <mat-label>Search products</mat-label>
        <input matInput [(ngModel)]="search" (keyup.enter)="reload(1)">
      </mat-form-field>
      <button class="btn-primary" (click)="reload(1)">Search</button>
    </div>

    @if (loading()) { <p>Loading...</p> }

    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      @for (p of products(); track p.id) {
        <mat-card class="card cursor-pointer" [routerLink]="['/products', p.id]">
          <mat-card-title class="text-lg font-semibold">{{ p.name }}</mat-card-title>
          <mat-card-subtitle class="text-slate-500">{{ p.no }}</mat-card-subtitle>
          <mat-card-content class="my-2 text-slate-700">{{ p.summary }}</mat-card-content>
          <div class="text-brand-700 font-semibold">\${{ p.price | number:'1.2-2' }}</div>
        </mat-card>
      }
    </div>

    <mat-paginator
      class="mt-4"
      [length]="total()"
      [pageIndex]="pageNumber() - 1"
      [pageSize]="pageSize()"
      [pageSizeOptions]="[10,20,50]"
      (page)="onPage($event)" />
  `,
})
export class ProductListComponent {
  private readonly api = inject(ProductService);

  readonly products = signal<Product[]>([]);
  readonly total = signal(0);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  search = '';

  constructor() { this.reload(1); }

  reload(page: number): void {
    this.loading.set(true);
    this.api.list(page, this.pageSize(), this.search || undefined).subscribe({
      next: (res) => {
        this.products.set(res.data);
        this.total.set(res.totalRecords);
        this.pageNumber.set(res.pageNumber);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.reload(event.pageIndex + 1);
  }
}

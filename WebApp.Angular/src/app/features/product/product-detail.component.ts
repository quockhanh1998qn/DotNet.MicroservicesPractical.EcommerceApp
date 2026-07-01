import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { ToastrService } from 'ngx-toastr';
import { ProductService } from '@core/services/product.service';
import { BasketService } from '@core/services/basket.service';
import { AuthService } from '@core/auth/auth.service';
import { Product } from '@core/models/product.model';

@Component({
  selector: 'tedu-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule],
  template: `
    @if (product(); as p) {
      <a routerLink="/products" class="btn-secondary inline-block mb-3">&larr; Back</a>
      <mat-card class="card max-w-2xl">
        <h1 class="text-2xl font-semibold">{{ p.name }}</h1>
        <p class="text-slate-500 mb-2">{{ p.no }}</p>
        <p class="mb-3">{{ p.description }}</p>
        <p class="text-xl text-brand-700 font-semibold mb-4">\${{ p.price | number:'1.2-2' }}</p>
        <button mat-flat-button color="primary" (click)="addToCart(p)">Add to cart</button>
      </mat-card>
    } @else {
      <p>Loading...</p>
    }
  `,
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ProductService);
  private readonly basket = inject(BasketService);
  private readonly auth = inject(AuthService);
  private readonly toastr = inject(ToastrService);

  readonly product = signal<Product | null>(null);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getById(id).subscribe((p) => this.product.set(p));
  }

  addToCart(p: Product): void {
    const id = this.auth.identity();
    if (!id) { this.auth.login(); return; }
    const username = id.email || id.sub;
    const existing = this.basket.cart();
    const items = [...(existing?.items ?? [])];
    const found = items.find((i) => i.productNo === p.no);
    if (found) { found.quantity += 1; }
    else { items.push({ productNo: p.no, productName: p.name, quantity: 1, price: p.price }); }
    this.basket.upsert({ username, items }).subscribe(() => this.toastr.success('Added to cart'));
  }
}

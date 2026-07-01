import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '@core/auth/auth.service';
import { BasketService } from '@core/services/basket.service';

interface CheckoutForm {
  firstName: string;
  lastName: string;
  emailAddress: string;
  shippingAddress: string;
  invoiceAddress: string;
  useShippingForInvoice: boolean;
}

@Component({
  selector: 'tedu-basket',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    MatTableModule, MatButtonModule, MatInputModule, MatFormFieldModule, MatCardModule,
  ],
  template: `
    <h1 class="text-2xl font-semibold mb-4">Your cart</h1>

    @if (cart(); as c) {
      @if (c.items.length === 0) {
        <p>Your cart is empty. <a routerLink="/products" class="text-brand-700 underline">Browse products</a></p>
      } @else {
        <table mat-table [dataSource]="c.items" class="card w-full mb-4">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>Product</th>
            <td mat-cell *matCellDef="let item">{{ item.productName }}</td>
          </ng-container>
          <ng-container matColumnDef="qty">
            <th mat-header-cell *matHeaderCellDef>Qty</th>
            <td mat-cell *matCellDef="let item">
              <input type="number" min="1" [(ngModel)]="item.quantity" class="w-16 border rounded px-2 py-1">
            </td>
          </ng-container>
          <ng-container matColumnDef="price">
            <th mat-header-cell *matHeaderCellDef>Price</th>
            <td mat-cell *matCellDef="let item">\${{ item.price | number:'1.2-2' }}</td>
          </ng-container>
          <ng-container matColumnDef="sub">
            <th mat-header-cell *matHeaderCellDef>Subtotal</th>
            <td mat-cell *matCellDef="let item">\${{ (item.price * item.quantity) | number:'1.2-2' }}</td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="cols"></tr>
          <tr mat-row *matRowDef="let row; columns: cols;"></tr>
        </table>

        <div class="flex items-center justify-between mb-6">
          <p class="text-lg">Total: <strong>\${{ total() | number:'1.2-2' }}</strong></p>
          <div class="flex gap-2">
            <button mat-stroked-button color="warn" (click)="clear()">Clear</button>
            <button mat-stroked-button color="primary" (click)="saveQuantities()">Save quantities</button>
            <button mat-flat-button color="primary" (click)="toggleCheckout()" [disabled]="submitting()">
              {{ showCheckout() ? 'Hide checkout form' : 'Checkout' }}
            </button>
          </div>
        </div>

        @if (showCheckout()) {
          <mat-card class="card mb-6">
            <h2 class="text-xl font-semibold mb-3">Shipping & billing</h2>
            <form #f="ngForm" (ngSubmit)="checkout(f)" class="grid grid-cols-1 md:grid-cols-2 gap-3">
              <mat-form-field appearance="outline">
                <mat-label>First name</mat-label>
                <input matInput name="firstName" required maxlength="100"
                       [(ngModel)]="form.firstName" data-testid="checkout-first-name">
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Last name</mat-label>
                <input matInput name="lastName" required maxlength="100"
                       [(ngModel)]="form.lastName" data-testid="checkout-last-name">
              </mat-form-field>
              <mat-form-field appearance="outline" class="md:col-span-2">
                <mat-label>Email</mat-label>
                <input matInput type="email" name="emailAddress" required email
                       [(ngModel)]="form.emailAddress" data-testid="checkout-email">
              </mat-form-field>
              <mat-form-field appearance="outline" class="md:col-span-2">
                <mat-label>Shipping address</mat-label>
                <textarea matInput name="shippingAddress" required maxlength="500" rows="2"
                          [(ngModel)]="form.shippingAddress" data-testid="checkout-shipping"></textarea>
              </mat-form-field>
              <label class="md:col-span-2 inline-flex items-center gap-2">
                <input type="checkbox" name="useShippingForInvoice"
                       [(ngModel)]="form.useShippingForInvoice" data-testid="checkout-same-billing">
                Billing address is the same as shipping
              </label>
              @if (!form.useShippingForInvoice) {
                <mat-form-field appearance="outline" class="md:col-span-2">
                  <mat-label>Billing address</mat-label>
                  <textarea matInput name="invoiceAddress" required maxlength="500" rows="2"
                            [(ngModel)]="form.invoiceAddress" data-testid="checkout-billing"></textarea>
                </mat-form-field>
              }
              <div class="md:col-span-2 flex justify-end">
                <button mat-flat-button color="primary" type="submit"
                        [disabled]="f.invalid || submitting()"
                        data-testid="checkout-submit">
                  Place order (\${{ total() | number:'1.2-2' }})
                </button>
              </div>
            </form>
          </mat-card>
        }
      }
    } @else {
      <p>Loading cart...</p>
    }
  `,
})
export class BasketComponent implements OnInit {
  private readonly basket = inject(BasketService);
  private readonly auth = inject(AuthService);
  private readonly toastr = inject(ToastrService);
  private readonly router = inject(Router);

  readonly cart = this.basket.cart;
  readonly cols = ['name', 'qty', 'price', 'sub'];
  readonly total = computed(() =>
    (this.cart()?.items ?? []).reduce((sum, item) => sum + item.price * item.quantity, 0),
  );

  readonly showCheckout = signal(false);
  readonly submitting = signal(false);

  readonly form: CheckoutForm = {
    firstName: '',
    lastName: '',
    emailAddress: '',
    shippingAddress: '',
    invoiceAddress: '',
    useShippingForInvoice: true,
  };

  ngOnInit(): void {
    const id = this.auth.identity();
    const username = id?.email;
    if (username) this.basket.load(username).subscribe();

    // Pre-fill from identity claims when available.
    if (id?.name) {
      const parts = id.name.split(' ');
      this.form.firstName = parts[0] ?? '';
      this.form.lastName = parts.slice(1).join(' ') || '-';
    }
    this.form.emailAddress = id?.email ?? '';
  }

  toggleCheckout(): void {
    this.showCheckout.update((v) => !v);
  }

  clear(): void {
    const username = this.auth.identity()?.email;
    if (!username) return;
    this.basket.remove(username).subscribe({
      next: () => {
        this.toastr.info('Cart cleared');
        this.showCheckout.set(false);
      },
      error: () => this.toastr.error('Could not clear the cart'),
    });
  }

  saveQuantities(): void {
    const c = this.cart();
    const username = this.auth.identity()?.email;
    if (!c || !username) return;
    this.basket.upsert({ username, items: c.items }).subscribe({
      next: () => this.toastr.success('Quantities updated'),
      error: (err: { error?: { errors?: Record<string, string[]> } }) => this.showServerErrors(err, 'Could not update the cart'),
    });
  }

  checkout(formRef: NgForm): void {
    if (formRef.invalid) return;
    const id = this.auth.identity();
    const c = this.cart();
    if (!id || !c || c.items.length === 0) return;

    const invoiceAddress = this.form.useShippingForInvoice
      ? this.form.shippingAddress
      : this.form.invoiceAddress;

    this.submitting.set(true);
    this.basket.checkout({
      username: id.email ?? id.sub,
      firstName: this.form.firstName.trim(),
      lastName: this.form.lastName.trim(),
      emailAddress: this.form.emailAddress.trim(),
      shippingAddress: this.form.shippingAddress.trim(),
      invoiceAddress: invoiceAddress.trim(),
      totalPrice: this.total(),
    }).subscribe({
      next: () => {
        this.toastr.success('Order submitted');
        this.submitting.set(false);
        this.showCheckout.set(false);
        // Server clears the basket after checkout; reload local view.
        const username = this.auth.identity()?.email;
        if (username) this.basket.load(username).subscribe();
        this.router.navigateByUrl('/orders');
      },
      error: (err: { error?: { errors?: Record<string, string[]> } }) => {
        this.submitting.set(false);
        this.showServerErrors(err, 'Checkout failed');
      },
    });
  }

  private showServerErrors(err: { error?: { errors?: Record<string, string[]> } }, fallback: string): void {
    const errors = err?.error?.errors;
    if (errors) {
      const lines = Object.values(errors).flat();
      this.toastr.error(lines.join('\n') || fallback);
    } else {
      this.toastr.error(fallback);
    }
  }
}

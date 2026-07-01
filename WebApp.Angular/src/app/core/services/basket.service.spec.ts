import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BasketService } from './basket.service';
import { Cart } from '../models/basket.model';
import { environment } from '@env';

describe('BasketService', () => {
  let service: BasketService;
  let http: HttpTestingController;

  const sampleCart: Cart = {
    username: 'alice@tedu.local',
    items: [
      { productId: 1, productName: 'A', quantity: 2, price: 10 },
      { productId: 2, productName: 'B', quantity: 1, price: 5 },
    ],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), BasketService],
    });
    service = TestBed.inject(BasketService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('starts with an empty cart and zero items', () => {
    expect(service.cart()).toBeNull();
    expect(service.itemCount()).toBe(0);
  });

  it('load() populates the cart signal and updates itemCount', () => {
    service.load('alice@tedu.local').subscribe();
    const req = http.expectOne(`${environment.apiBase}/basket/alice@tedu.local`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleCart);

    expect(service.cart()).toEqual(sampleCart);
    expect(service.itemCount()).toBe(3);
  });

  it('upsert() posts cart and stores response', () => {
    service.upsert(sampleCart).subscribe();
    const req = http.expectOne(`${environment.apiBase}/basket`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(sampleCart);
    req.flush({ ...sampleCart, totalPrice: 25 });

    expect(service.cart()?.totalPrice).toBe(25);
  });

  it('remove() clears the local cart signal', () => {
    service.load('alice@tedu.local').subscribe();
    http.expectOne(`${environment.apiBase}/basket/alice@tedu.local`).flush(sampleCart);
    expect(service.cart()).not.toBeNull();

    service.remove('alice@tedu.local').subscribe();
    const del = http.expectOne(`${environment.apiBase}/basket/alice@tedu.local`);
    expect(del.request.method).toBe('DELETE');
    del.flush(null);

    expect(service.cart()).toBeNull();
    expect(service.itemCount()).toBe(0);
  });

  it('checkout() posts to the checkout endpoint', () => {
    service.checkout({
      username: 'alice@tedu.local',
      firstName: 'Alice', lastName: 'A',
      emailAddress: 'alice@tedu.local',
      shippingAddress: 'addr-ship', invoiceAddress: 'addr-inv',
      totalPrice: 25,
    }).subscribe();
    const req = http.expectOne(`${environment.apiBase}/basket/checkout`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.totalPrice).toBe(25);
    req.flush(null);
  });
});

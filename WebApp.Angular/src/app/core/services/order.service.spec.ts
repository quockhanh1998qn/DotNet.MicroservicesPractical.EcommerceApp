import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { OrderService } from './order.service';
import { environment } from '@env';

describe('OrderService', () => {
  let service: OrderService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), OrderService],
    });
    service = TestBed.inject(OrderService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listByUser() GETs /orders/{username}', () => {
    service.listByUser('alice@tedu.local').subscribe((orders) => {
      expect(orders.length).toBe(1);
      expect(orders[0].id).toBe(7);
    });
    const req = http.expectOne(`${environment.apiBase}/orders/alice@tedu.local`);
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 7, userName: 'alice', totalPrice: 42, status: 1, createdDate: '2026-01-01' }]);
  });

  it('getById() GETs /orders/detail/{id}', () => {
    service.getById(99).subscribe((o) => expect(o.id).toBe(99));
    const req = http.expectOne(`${environment.apiBase}/orders/detail/99`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 99, userName: 'x', totalPrice: 1, status: 0, createdDate: '2026-01-01' });
  });
});

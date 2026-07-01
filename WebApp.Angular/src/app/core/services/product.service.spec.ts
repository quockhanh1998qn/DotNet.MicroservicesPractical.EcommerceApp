import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ProductService } from './product.service';
import { environment } from '@env';

describe('ProductService', () => {
  let service: ProductService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ProductService],
    });
    service = TestBed.inject(ProductService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists products with paging params', () => {
    service.list(2, 25, 'phone').subscribe();
    const req = http.expectOne((r) => r.url === `${environment.apiBase}/products`);
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.get('search')).toBe('phone');
    req.flush({ pageNumber: 2, pageSize: 25, totalRecords: 0, totalPages: 0, data: [] });
  });

  it('fetches single product by id', () => {
    service.getById(42).subscribe((p) => expect(p.id).toBe(42));
    const req = http.expectOne(`${environment.apiBase}/products/42`);
    req.flush({ id: 42, no: 'P-042', name: 'Sample', price: 9.99 });
  });
});

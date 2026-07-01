import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from './auth.service';
import { errorInterceptor } from './error.interceptor';

class ToastrStub {
  warning = jest.fn();
  error = jest.fn();
  info = jest.fn();
  success = jest.fn();
}

class AuthStub {
  login = jest.fn();
}

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let toastr: ToastrStub;
  let auth: AuthStub;

  beforeEach(() => {
    toastr = new ToastrStub();
    auth = new AuthStub();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastrService, useValue: toastr },
        { provide: AuthService, useValue: auth },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('on 401: warns and triggers login()', () => {
    http.get('/api/x').subscribe({ error: () => void 0 });
    httpMock.expectOne('/api/x').flush('', { status: 401, statusText: 'Unauthorized' });
    expect(toastr.warning).toHaveBeenCalledTimes(1);
    expect(auth.login).toHaveBeenCalledTimes(1);
  });

  it('on 403: shows a permission error toast', () => {
    http.get('/api/x').subscribe({ error: () => void 0 });
    httpMock.expectOne('/api/x').flush('', { status: 403, statusText: 'Forbidden' });
    expect(toastr.error).toHaveBeenCalledWith('You do not have permission to perform this action.');
    expect(auth.login).not.toHaveBeenCalled();
  });

  it('on 500: shows a generic server error toast', () => {
    http.get('/api/x').subscribe({ error: () => void 0 });
    httpMock.expectOne('/api/x').flush('', { status: 500, statusText: 'Server Error' });
    expect(toastr.error).toHaveBeenCalledWith('Server error. Please try again later.');
  });

  it('on network failure (status 0): shows network error toast', () => {
    http.get('/api/x').subscribe({ error: () => void 0 });
    httpMock.expectOne('/api/x').error(new ProgressEvent('error'));
    expect(toastr.error).toHaveBeenCalledWith('Network error. Check your connection.');
  });
});

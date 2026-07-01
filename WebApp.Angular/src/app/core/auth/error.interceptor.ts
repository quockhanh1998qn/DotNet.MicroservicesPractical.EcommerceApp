import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const errorInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const toastr = inject(ToastrService);
  const auth = inject(AuthService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        toastr.warning('Your session expired. Please sign in again.');
        auth.login();
      } else if (err.status === 403) {
        toastr.error('You do not have permission to perform this action.');
      } else if (err.status >= 500) {
        toastr.error('Server error. Please try again later.');
      } else if (err.status === 0) {
        toastr.error('Network error. Check your connection.');
      } else {
        toastr.error(err.error?.title ?? err.message ?? 'Request failed.');
      }
      return throwError(() => err);
    }),
  );
};

import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'tedu-auth-callback',
  standalone: true,
  template: `<p class="text-center mt-10 text-slate-600">Signing you in...</p>`,
})
export class AuthCallbackComponent implements OnInit {
  private readonly router = inject(Router);
  ngOnInit(): void {
    // OAuthService.loadDiscoveryDocumentAndTryLogin (called in app initializer)
    // already processes the code grant on this URL. Just bounce to home.
    this.router.navigate(['/products']);
  }
}

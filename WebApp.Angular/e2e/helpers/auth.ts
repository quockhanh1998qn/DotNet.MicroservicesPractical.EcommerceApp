import { expect, Page } from '@playwright/test';

export const DEFAULT_ADMIN = { email: 'admin@tedu.local', password: 'Admin@123!' };
export const DEFAULT_CUSTOMER = { email: 'customer@tedu.local', password: 'Customer@123!' };

/**
 * Sign in via the Tedu shop toolbar -> Identity Server login form -> redirect back.
 * Assumes the dev server is running at `baseURL` (configured in playwright.config.ts).
 */
export async function signIn(page: Page, user = DEFAULT_CUSTOMER): Promise<void> {
  await page.goto('/products');

  // Idempotent: if already signed in we'll see the email instead of the Sign in button.
  const signInButton = page.getByRole('button', { name: /sign in/i });
  if (!(await signInButton.isVisible().catch(() => false))) {
    return;
  }

  await signInButton.click();

  // Wait for the Identity Server login form to render (URL casing varies, so wait on the form).
  const emailField = page.locator('input[name="Email"]');
  await emailField.waitFor({ state: 'visible', timeout: 20_000 });
  await emailField.fill(user.email);
  await page.locator('input[name="Password"]').fill(user.password);
  await page.locator('form button[type="submit"]').click();

  // Identity Server completes the auth-code+pkce dance and bounces back to /auth-callback,
  // which the AuthCallbackComponent then routes to /products.
  await page.waitForURL(/localhost:4200\/products/i, { timeout: 25_000 });
  await expect(page.getByRole('button', { name: /sign out/i })).toBeVisible({ timeout: 10_000 });
}

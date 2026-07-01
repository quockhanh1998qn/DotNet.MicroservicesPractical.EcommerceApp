import { expect, test } from '@playwright/test';
import { DEFAULT_CUSTOMER, signIn } from './helpers/auth';

/**
 * Full shopper happy path:
 *   sign in -> browse products -> open product detail -> add to cart
 *   -> review cart -> checkout with shipping form -> land on orders page
 *   -> assert order exists.
 */
test.describe('shopper journey', () => {
  test.beforeEach(async ({ page }) => {
    await signIn(page, DEFAULT_CUSTOMER);
  });

  test('user can add a product to the cart and place an order', async ({ page }) => {
    // 1. signIn already navigates to /products; just wait for the product cards to render.
    const firstCard = page.locator('mat-card.card').first();
    await expect(firstCard).toBeVisible({ timeout: 15_000 });
    const productName = (await firstCard.locator('mat-card-title').innerText()).trim();
    await firstCard.click();

    // 2. Product detail renders and we can add it to the cart.
    await expect(page).toHaveURL(/\/products\/\d+/);
    await expect(page.getByRole('button', { name: /add to cart/i })).toBeVisible();
    await page.getByRole('button', { name: /add to cart/i }).click();

    // Wait briefly for the POST /Baskets to complete before navigating.
    await page.waitForResponse(
      (resp) => resp.url().includes('/baskets') && resp.request().method() === 'POST' && resp.ok(),
      { timeout: 10_000 },
    );

    // 3. Cart now shows the item.
    await page.goto('/basket');
    await expect(page.locator('text=Your cart')).toBeVisible();
    await expect(page.locator(`text=${productName}`)).toBeVisible({ timeout: 10_000 });

    // 4. Open checkout form and fill required fields.
    await page.getByRole('button', { name: /^checkout$/i }).click();
    await page.locator('[data-testid="checkout-first-name"]').fill('Tester');
    await page.locator('[data-testid="checkout-last-name"]').fill('McAutomated');
    await page.locator('[data-testid="checkout-email"]').fill(DEFAULT_CUSTOMER.email);
    await page.locator('[data-testid="checkout-shipping"]').fill('1 Test Street, Test City');

    // 5. Submit checkout and wait for the API call.
    const checkoutResponse = page.waitForResponse(
      (resp) => resp.url().includes('/baskets/checkout') && resp.request().method() === 'POST',
      { timeout: 15_000 },
    );
    await page.locator('[data-testid="checkout-submit"]').click();
    const resp = await checkoutResponse;
    expect(resp.status()).toBe(202);

    // 6. Lands on /orders. The MassTransit consumer creates the order asynchronously,
    // so reload until at least one row appears (or the well-known empty state stays).
    await page.waitForURL(/\/orders$/, { timeout: 15_000 });
    await expect(page.locator('text=Your orders')).toBeVisible();
    for (let i = 0; i < 6; i++) {
      const rows = await page.locator('table tbody tr').count();
      if (rows > 0) break;
      await page.waitForTimeout(1_000);
      await page.reload();
    }
    await expect(page.locator('table tbody tr').first()).toBeVisible({ timeout: 5_000 });
  });

  test('cart page is reachable after sign-in', async ({ page }) => {
    await page.goto('/basket');
    // The cart page either shows the empty-state message or the cart table; we
    // assert on the title heading which is always present.
    await expect(page.getByRole('heading', { name: /your cart/i })).toBeVisible();
  });
});

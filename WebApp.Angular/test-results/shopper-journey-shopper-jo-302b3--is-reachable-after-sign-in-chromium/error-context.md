# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: shopper-journey.spec.ts >> shopper journey >> cart page is reachable after sign-in
- Location: e2e\shopper-journey.spec.ts:67:7

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByRole('heading', { name: /your cart/i })
Expected: visible
Timeout: 10000ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 10000ms
  - waiting for getByRole('heading', { name: /your cart/i })
    - waiting for" http://localhost:5009/Account/Login?ReturnUrl=%2Fconnect%2Fauthorize%2Fcallback%3Fresponse_type%3Dcode%26client_id%3Dwebapp_angular%26state%3DdjJ-dDJ-NF9uampRSngzaHdwU2FOMWNxZTVDSko2NzZQb2U5T1lyZDZsY…" navigation to finish...
    - navigated to "http://localhost:5009/Account/Login?ReturnUrl=%2Fconnect%2Fauthorize%2Fcallback%3Fresponse_type%3Dcode%26client_id%3Dwebapp_angular%26state%3DdjJ-dDJ-NF9uampRSngzaHdwU2FOMWNxZTVDSko2NzZQb2U5T1lyZDZsY…"

```

```yaml
- text: Tedu Microservices
- heading "Sign in to your account" [level=1]
- text: Email
- textbox "Email"
- text: Password
- textbox "Password"
- checkbox "Remember me"
- text: Remember me
- button "Sign in"
- strong: Dev users
- code: admin@tedu.local
- text: /
- code: Admin@123!
- code: customer@tedu.local
- text: /
- code: Customer@123!
```

# Test source

```ts
  1  | import { expect, test } from '@playwright/test';
  2  | import { DEFAULT_CUSTOMER, signIn } from './helpers/auth';
  3  | 
  4  | /**
  5  |  * Full shopper happy path:
  6  |  *   sign in -> browse products -> open product detail -> add to cart
  7  |  *   -> review cart -> checkout with shipping form -> land on orders page
  8  |  *   -> assert order exists.
  9  |  */
  10 | test.describe('shopper journey', () => {
  11 |   test.beforeEach(async ({ page }) => {
  12 |     await signIn(page, DEFAULT_CUSTOMER);
  13 |   });
  14 | 
  15 |   test('user can add a product to the cart and place an order', async ({ page }) => {
  16 |     // 1. signIn already navigates to /products; just wait for the product cards to render.
  17 |     const firstCard = page.locator('mat-card.card').first();
  18 |     await expect(firstCard).toBeVisible({ timeout: 15_000 });
  19 |     const productName = (await firstCard.locator('mat-card-title').innerText()).trim();
  20 |     await firstCard.click();
  21 | 
  22 |     // 2. Product detail renders and we can add it to the cart.
  23 |     await expect(page).toHaveURL(/\/products\/\d+/);
  24 |     await expect(page.getByRole('button', { name: /add to cart/i })).toBeVisible();
  25 |     await page.getByRole('button', { name: /add to cart/i }).click();
  26 | 
  27 |     // Wait briefly for the POST /Baskets to complete before navigating.
  28 |     await page.waitForResponse(
  29 |       (resp) => resp.url().includes('/baskets') && resp.request().method() === 'POST' && resp.ok(),
  30 |       { timeout: 10_000 },
  31 |     );
  32 | 
  33 |     // 3. Cart now shows the item.
  34 |     await page.goto('/basket');
  35 |     await expect(page.locator('text=Your cart')).toBeVisible();
  36 |     await expect(page.locator(`text=${productName}`)).toBeVisible({ timeout: 10_000 });
  37 | 
  38 |     // 4. Open checkout form and fill required fields.
  39 |     await page.getByRole('button', { name: /^checkout$/i }).click();
  40 |     await page.locator('[data-testid="checkout-first-name"]').fill('Tester');
  41 |     await page.locator('[data-testid="checkout-last-name"]').fill('McAutomated');
  42 |     await page.locator('[data-testid="checkout-email"]').fill(DEFAULT_CUSTOMER.email);
  43 |     await page.locator('[data-testid="checkout-shipping"]').fill('1 Test Street, Test City');
  44 | 
  45 |     // 5. Submit checkout and wait for the API call.
  46 |     const checkoutResponse = page.waitForResponse(
  47 |       (resp) => resp.url().includes('/baskets/checkout') && resp.request().method() === 'POST',
  48 |       { timeout: 15_000 },
  49 |     );
  50 |     await page.locator('[data-testid="checkout-submit"]').click();
  51 |     const resp = await checkoutResponse;
  52 |     expect(resp.status()).toBe(202);
  53 | 
  54 |     // 6. Lands on /orders. The MassTransit consumer creates the order asynchronously,
  55 |     // so reload until at least one row appears (or the well-known empty state stays).
  56 |     await page.waitForURL(/\/orders$/, { timeout: 15_000 });
  57 |     await expect(page.locator('text=Your orders')).toBeVisible();
  58 |     for (let i = 0; i < 6; i++) {
  59 |       const rows = await page.locator('table tbody tr').count();
  60 |       if (rows > 0) break;
  61 |       await page.waitForTimeout(1_000);
  62 |       await page.reload();
  63 |     }
  64 |     await expect(page.locator('table tbody tr').first()).toBeVisible({ timeout: 5_000 });
  65 |   });
  66 | 
  67 |   test('cart page is reachable after sign-in', async ({ page }) => {
  68 |     await page.goto('/basket');
  69 |     // The cart page either shows the empty-state message or the cart table; we
  70 |     // assert on the title heading which is always present.
> 71 |     await expect(page.getByRole('heading', { name: /your cart/i })).toBeVisible();
     |                                                                     ^ Error: expect(locator).toBeVisible() failed
  72 |   });
  73 | });
  74 | 
```
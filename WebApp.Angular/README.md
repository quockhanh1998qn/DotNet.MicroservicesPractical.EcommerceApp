# Tedu WebApp (Angular 17)

End-user storefront for the Tedu Microservices solution.

- **Framework:** Angular 17 standalone + signals + lazy routes
- **UI:** TailwindCSS 3 + Angular Material 17
- **Auth:** angular-oauth2-oidc (OIDC code + PKCE) against Identity Server (Duende, port 5009)
- **API gateway:** http://localhost:5000 (Ocelot)
- **Toast:** ngx-toastr
- **Tests:** Jest via `@angular-builders/jest`

## Develop locally

```powershell
cd WebApp.Angular
npm install
npm start             # ng serve --port 4200
npm test              # jest
npm run build         # production bundle to dist/tedu-webapp/browser
```

Requires the backend stack (Identity Server + Ocelot + APIs) to be running:

```powershell
docker compose up -d --build identity.server ocelot.gateway product.api customer.api basket.api ordering.api inventory.api
```

## Features

- **Products** — paginated list with search, detail view, add-to-cart
- **Cart** — view, update quantity, clear, checkout (POST `/basket/checkout`)
- **Orders** — list current user's orders, drill-down detail
- **Auth** — sign in/out, silent token refresh, route guards (`authGuard`, `adminGuard`)
- **Errors** — global `errorInterceptor` surfaces 401/403/5xx via toast

## Architecture

```
src/app/
  core/
    auth/            (AuthService, interceptors, guards)
    services/        (ProductService, BasketService, OrderService)
    models/          (Product, Cart, Order)
  features/
    product/         (list + detail)
    basket/          (cart + checkout)
    order/           (list + detail)
    auth/            (OIDC callback + silent renew)
  app.component.ts   (root shell + Material toolbar)
  app.config.ts      (providers, APP_INITIALIZER for AuthService)
  app.routes.ts      (lazy routes + guards)
```

## Container

```powershell
docker compose up -d --build webapp.angular
# Open http://localhost:4200
```

The container is a multi-stage Node build → nginx serving the production bundle.

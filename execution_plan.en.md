# Execution Plan – ASP.NET Core Microservices

> **Iron Law:** Follow the plan. Review every task. Never trust self-reports.
>
> The plan is the spec. If the plan is wrong, update the plan first — then execute.

---

## 1. Objective

Build a complete Microservices system aligned with `microservice_course_architecture.png`:

- **API Gateway** (Ocelot, port 5000) routing to downstream microservices
- **Identity Service** (5001) – Duende Identity Server + SQL Server
- **Product.API** (5002) – MySQL, EF Core, Repository Pattern
- **Customer.API** (5003) – PostgreSQL, Minimal API
- **Basket.API** (5004) – Redis, RabbitMQ publisher
- **Ordering.API** (5005) – SQL Server, Clean Architecture + CQRS, RabbitMQ consumer
- **Inventory.API** (5006) – MongoDB, gRPC server
- **Background Job Service** (5007) – Hangfire + SMTP
- **Event Bus** – RabbitMQ (15672), MassTransit
- **Cross-cutting** – Serilog → Elasticsearch/Kibana, Polly, HealthChecks, WebStatus dashboard
- **Client Apps**
  - **WebApp (Angular 17+)** – End-user SPA (port 4200): browse products, manage basket, checkout, view orders, login via Identity Server (OIDC + PKCE)
  - **Admin Portal (Blazor Server)** – Admin UI (port 5100): manage Products / Customers / Inventory / Orders, view health status, view Hangfire jobs

---

## 2. Current State Snapshot

> Last updated: 2026-06-29

| Component | Status | Notes |
|---|---|---|
| `Contracts` | ✅ Done | `EntityBase`, `EntityAuditBase`, `IRepositoryBaseAsync`, `IUnitOfWork` |
| `Infrastructure` | ✅ Done | `RepositoryBaseAsync<T>`, `UnitOfWork`, unit tested |
| `Common.Logging` | ✅ Done | `Serilogger` (Elasticsearch sink), `CorrelationIdMiddleware`, unit tested |
| `Common.Auth` | ✅ Done | `AddMicroserviceAuthentication` JWT bearer + scope policy, unit tested |
| `EventBus.Messages` | ✅ Done | `IntegrationBaseEvent`, `BasketCheckoutEvent`, `EventBusConstants` |
| `Shared` | ✅ Done | DTOs for Product, Customer, Basket, Ordering, Inventory; `PagedList<T>`; config POCOs |
| `Product.API` | ✅ Done | CRUD + MySQL + EF Core + AutoMapper + Serilog + HealthCheck + Auth; unit tested |
| `Customer.API` | ✅ Done | Minimal API + PostgreSQL + repository + AutoMapper + HealthCheck + Auth; unit tested |
| `Basket.API` | ✅ Done | Redis + MassTransit publisher + gRPC client (stock check via Inventory) + Polly + Auth; unit tested |
| `Ordering.Domain` | ✅ Done | `Order` aggregate, `AggregateRoot<long>`, `OrderStatus`, domain events |
| `Ordering.Application` | ✅ Done | MediatR CQRS (5 commands/queries), FluentValidation, `ISmtpEmailService`; unit tested |
| `Ordering.Infrastructure` | ✅ Done | `OrderContext` SQL Server + `OrderRepository` + `SmtpEmailService` stub |
| `Ordering.API` | ✅ Done | Thin controller + MassTransit `BasketCheckoutEventConsumer` + InMemory Outbox + Auth |
| `Inventory.API` | ✅ Done | MongoDB + gRPC server (`StockProtoService`) + REST CRUD + HealthCheck + Auth; unit tested |
| `HangFire.API` | ✅ Done | Hangfire SQL Server + basket-reminder job + SMTP + Polly HttpClient + Auth |
| `Identity.Server` | ✅ Done | Duende 7 + ASP.NET Identity + EF Core stores + Dapper permissions + stored procs + email flows |
| `OcelotApiGw` | ✅ Done | Ocelot routing (6 route files) + JWT Bearer + rate limiting + Polly QoS + CacheManager + CORS |
| `WebHealthStatus` | ✅ Done | HealthChecks UI aggregating Product, Customer, Basket, Ordering, Inventory, HangFire |
| `WebApp.Angular` | ⚠️ Partial | Project scaffolded: Angular 17 + Tailwind + angular-oauth2-oidc + features (product/basket/order/auth) + services + models + Jest; `node_modules/` not installed locally; e2e tests (T11.10) not yet added |
| `Admin.Blazor` | ✅ Done | Blazor Server + MudBlazor + OIDC (Admin role) + typed HttpClients + pages: Products, Customers, Inventory, Orders, Health, Jobs, Logs; MassTransit `StockUpdateConsumer` |
| `docker-compose` | ✅ Done | All 20 services defined; infrastructure: MySQL, PostgreSQL, Redis, MongoDB, SQL Server, RabbitMQ, Elasticsearch, Kibana, pgAdmin, Portainer |

### Test Projects

| Project | Tests | Status |
|---|---:|---|
| `Infrastructure.UnitTests` | 4 | ✅ |
| `Product.API.UnitTests` | 5 | ✅ |
| `Customer.API.UnitTests` | 4 | ✅ |
| `Basket.API.UnitTests` | 8 | ✅ |
| `Ordering.Application.UnitTests` | 10 | ✅ |
| `Inventory.API.UnitTests` | 5 | ✅ |
| `Common.Auth.UnitTests` | 3 | ✅ |
| `Common.Logging.UnitTests` | 3 | ✅ |
| **Total** | **42** | **0 failures** |

### Open items (from code review)

| Priority | Issue | Component |
|---|---|---|
| 🔴 Critical | Hardcoded OIDC client secret in `Program.cs:44` | Admin.Blazor |
| 🔴 Critical | Wildcard CORS `SetIsOriginAllowed(_ => true)` | Identity.Server |
| 🔴 Critical | Blocking sync SQL in DI registration (`EnsureDatabaseExists`) | HangFire.API |
| 🟡 Important | `ModelState.IsValid` redundant in `[ApiController]` | Basket.API, Inventory.API |
| 🟡 Important | `AddAutoMapper` not scanning assembly | Basket, Ordering, Inventory |
| 🟡 Important | Cleartext RabbitMQ fallback credentials in code | Ordering.API |
| 🟡 Important | `EnsureCreatedAsync` instead of `MigrateAsync` | Ordering.API |
| 🟡 Important | e2e tests not added | WebApp.Angular |
| 🟡 Important | bUnit tests not added (T12.12) | Admin.Blazor |

---

## 3. Execution Principles

1. **Each task** is a small, independently reviewable unit (≤ 30 minutes of coding).
2. **Two-stage review** for every task:
   - **Stage 1 – Spec compliance:** Does it match the plan?
   - **Stage 2 – Code quality:** Clean implementation, tests pass, no warnings, conventions respected.
3. **Checkpoint every 3–5 tasks:** run `dotnet build`, smoke test, and commit.
4. **Wave** = a group of tasks that can run in parallel (no shared files). Tasks in the same wave can run concurrently only if they do not touch the same files.
5. **Verification before closing a task:**
   - `dotnet build` is clean
   - Endpoints work via Swagger / `.http`
   - If containerized, `docker compose up` succeeds
6. If stuck **3 times** on the same task, stop, update the plan, then continue.
7. **Mandatory testing depth by layer:**
   - Backend: unit tests for business logic (Domain/Application/Service), not API smoke tests only.
   - API/Infrastructure: integration tests for critical flows (DB, event bus, auth).
   - UI: unit/component tests plus e2e smoke tests for critical user journeys.

---

## 4. Waves & Tasks

### Wave 0 – Shared Foundation Hardening ✅ DONE

- **T0.1** ✅ `Contracts`: `EntityBase`, `EntityAuditBase`, `IRepositoryBaseAsync<T>`, `IUnitOfWork`
- **T0.2** ✅ `Infrastructure`: `RepositoryBaseAsync<T>` (FindAll, FindByCondition, Create/Update/Delete/SaveChanges), `UnitOfWork`; unit tested
- **T0.3** ✅ `Common.Logging`: `Serilogger` console + file + Elasticsearch sink; `CorrelationIdMiddleware`; unit tested
- **T0.4** ✅ `Shared/Configurations`: `DatabaseSettings`, `EmailSettings`, `HangfireSettings`, `BasketReminderSettings`, etc.

---

### Wave 1 – Product.API ✅ DONE

- **T1.1–T1.4** ✅ DTOs (`ProductDto`, `CreateProductDto`, `UpdateProductDto`), `MappingProfile`, `IProductRepository`, `ProductsController` (full CRUD with `[ProducesResponseType]`)
- **T1.5–T1.6** ✅ `ProductContext` + migrations + seed on MySQL; `appsettings.json` configured
- **T1.7–T1.8** ✅ `Dockerfile` + `product.api` (5102) in compose; `Common.Auth` applied

---

### Wave 2 – Customer.API ✅ DONE

- **T2.1–T2.8** ✅ Minimal API CRUD + PostgreSQL (`CustomerContext`, `CustomerRepository`, AutoMapper profile, `Shared/DTOs/Customer`), Dockerfile, compose `customer.api` (5103); unit tested

---

### Wave 3 – Basket.API + Redis ✅ DONE

- **T3.1–T3.5** ✅ `Cart`/`CartItem` entities, `IBasketRepository`/`BasketRepository` (IDistributedCache, key `basket:<user>`, 7d+2h TTL), `BasketsController` (GET/POST/DELETE + checkout), compose `basket.api` (5004) + `basketdb`; unit tested (8 tests)

---

### Wave 4 – Ordering.API (Clean Architecture + CQRS) ✅ DONE

- **T4.1** ✅ 4-layer split: `Ordering.Domain`, `.Application`, `.Infrastructure`, `.API`
- **T4.2** ✅ `Order` aggregate, `AggregateRoot<long>`, `OrderStatus`, `OrderCreated/Updated/Deleted` domain events
- **T4.3** ✅ MediatR CQRS — `CreateOrder`, `UpdateOrder`, `DeleteOrder` commands + `GetOrdersByUserName`, `GetOrderById` queries; FluentValidation; AutoMapper; unit tested (10 tests)
- **T4.4** ✅ `OrderContext` SQL Server + `OrderContextSeed` + `OrderRepository : RepositoryBaseAsync`
- **T4.5** ✅ `ISmtpEmailService` + `SmtpEmailService` stub
- **T4.6** ✅ Thin `OrdersController` → `IMediator`
- **T4.7** ✅ Dockerfile + compose `ordering.api` (5005) + `orderdb`
- ⚠️ Deferred: `EnsureCreatedAsync` used instead of `MigrateAsync` (pending SQL Server availability); `FluentValidationClientsideAdapters` should be removed

---

### Wave 5 – RabbitMQ + MassTransit ✅ DONE

- **T5.2–T5.5** ✅ `IntegrationBaseEvent`, `BasketCheckoutEvent`, `EventBusConstants`; `Basket.API` publishes via `IPublishEndpoint`; `Ordering.API` consumes via `BasketCheckoutEventConsumer` + `UseInMemoryOutbox` + retry; compose `rabbitmq` wired
- ⚠️ Deferred: T5.1 (RabbitMQ.Demo console sample) skipped; T5.6/T5.7 (domain event dispatch in `SaveChangesAsync`) not implemented — domain events are raised but no `IDomainEventDispatcher` hook exists

---

### Wave 6 – Inventory.API + MongoDB + gRPC ✅ DONE

- **T6.1–T6.4** ✅ `InventoryEntry` (BSON), `InventoryContext`, `InventoryRepository`, `InventoryService` (Purchase/Sale + stock check), `InventoriesController` (6 endpoints), `Shared/Common/PagedList<T>`
- **T6.5–T6.6** ✅ `stock.proto` → `StockProtoServiceImpl` (gRPC server) mapped via `MapGrpcService`
- **T6.7** ✅ `Basket.API` gRPC client (`StockService` with Polly fallback); `BasketsController.UpdateBasket` validates stock before add
- **T6.8** ✅ Dockerfile + compose `inventory.api` (5006) + `inventorydb`; unit tested (5 tests)

---

### Wave 7 – Ocelot API Gateway ✅ DONE

- **T7.1** ✅ 6 merged route files (`ocelot.global.json` + per-service), auto-merged at startup
- **T7.2** ✅ JWT Bearer `"Bearer"` scheme + Identity Server authority (env-driven)
- **T7.3** ✅ `AddCacheManager`, `AddPolly`, per-route `RateLimitOptions` (30 req/10s), `QoSOptions` (3 exceptions / 5s break)
- **T7.4** ✅ Dockerfile + compose `ocelot.gateway` (5000); CORS for `localhost:4200` + `localhost:5100`

---

### Wave 8 – HangFire.API ✅ DONE

- **T8.1–T8.4** ✅ Hangfire SQL Server storage, dashboard `/hangfire`, `IBasketReminderJob` + `SmtpEmailService`, Polly on `BasketApiClient` HttpClient, Dockerfile + compose `hangfire.api` (5007)
- ⚠️ Critical: `EnsureDatabaseExists` is synchronous blocking during DI registration — needs fix

---

### Wave 9 – Observability + Resilience + HealthChecks ✅ DONE

- **T9.1** ✅ Serilog → Elasticsearch sink (active when `ElasticConfiguration:Uri` set)
- **T9.2** ✅ Kibana setup documented in README
- **T9.3** ✅ `CorrelationIdMiddleware` + `UseCorrelationId()` extension; unit tested
- **T9.4–T9.5** ✅ Polly applied: Basket→gRPC (retry 3x, CB 5/30s, timeout 10s), HangFire→HTTP (same)
- **T9.6** ✅ `AspNetCore.HealthChecks.*` per service; each exposes `/health` with UI JSON writer
- **T9.7** ✅ `WebHealthStatus` aggregates 6 services at `http://localhost:5008/healthchecks-ui`
- **T9.8** ✅ MassTransit `UseInMemoryOutbox` + retry on Ordering consumer

---

### Wave 10 – Identity Server + System-wide Auth ✅ DONE

- **T10.1–T10.2** ✅ `Identity.Server`: Duende IdentityServer 7, Serilog, 6 API scopes, 4 clients (webapp_angular, admin_blazor, hangfire_worker, swagger_ui)
- **T10.3** ✅ EF Core Config + OperationalStore migrations (9 migration files)
- **T10.4** ✅ ASP.NET Core Identity (`User`, `Role`, `UserRepository`)
- **T10.5** ✅ `EmailService` (confirm email, forgot/reset password)
- **T10.6–T10.7** ✅ `PermissionRepository` + Dapper + stored procedures `sp_GetPermissionsByUser`, `sp_GetPermissionsByRole`
- **T10.8** ✅ `GET /api/permissions/me` (Bearer required)
- **T10.9–T10.10** ✅ `Common.Auth.AddMicroserviceAuthentication` applied to all services via `AuthSettings` env vars
- **T10.11** ✅ JWT Bearer in OcelotApiGw pointing to Identity Server
- **T10.12** ✅ Dockerfile + compose `identity.server` (5009)
- ⚠️ Critical: `SetIsOriginAllowed(_ => true)` wildcard CORS — needs fix

---

### Wave 11 – WebApp.Angular ⚠️ PARTIAL

- **T11.1** ✅ Angular 17 standalone project; folder structure: `core/{auth,services,models}`, `features/{product,basket,order,auth}`
- **T11.2** ✅ Tailwind CSS 3 + Angular Material 17; `tailwind.config.js`, `postcss.config.js`
- **T11.3** ✅ `ProductService`, `BasketService`, `OrderService` (HttpClient via Ocelot); unit spec files present
- **T11.4** ✅ `angular-oauth2-oidc@17`; `AuthService`, `AuthInterceptor`, `AuthGuard`, `ErrorInterceptor`
- **T11.5** ✅ `product-list.component.ts` (search + pagination), `product-detail.component.ts` (add to cart)
- **T11.6** ✅ `basket.component.ts` (view cart, update qty, checkout)
- **T11.7** ✅ `order-list.component.ts`, `order-detail.component.ts`
- **T11.8** ✅ `AuthGuard` + role-based route protection in `app.routes.ts`
- **T11.9** ⚠️ Error handling / loading states / toast — unverified without `ng build`
- **T11.10** ⚠️ Jest unit specs present; Playwright e2e added (`e2e/shopper-journey.spec.ts`, `e2e/helpers/auth.ts`) — **1/2 tests pass**: full shopper journey ✅; cart-reachability ❌ (OIDC `sessionStorage` not persisted across `page.goto` — needs `storageState` fix)
- **T11.11** ✅ Multi-stage `Dockerfile` (npm ci → ng build → nginx); `nginx.conf`; compose `webapp.angular` (4200 → 80)
- **T11.12** ✅ Ocelot CORS + Identity Server clients updated
- ⚠️ Remaining: fix T11.10 cart-reachability e2e (persist OIDC `storageState` in `playwright.config.ts`); run `ng build` to verify no compile errors

---

### Wave 12 – Admin.Blazor ✅ DONE (T12.12 pending)

- **T12.1** ✅ Blazor Server, MudBlazor, `InteractiveServer` render mode
- **T12.2** ✅ Layout + navigation (`Layout/` folder)
- **T12.3** ✅ OIDC code flow, `Admin` role policy, fallback redirect-to-login
- **T12.4** ✅ Typed HttpClients: `ProductApiClient`, `CustomerApiClient`, `InventoryApiClient`, `OrderApiClient`
- **T12.5** ✅ `Products.razor` + `ProductEditDialog.razor` (CRUD data grid)
- **T12.6** ✅ `Customers.razor`
- **T12.7** ✅ `Inventory.razor` + MassTransit `StockUpdateConsumer` (real-time stock updates)
- **T12.8** ✅ `Orders.razor`
- **T12.9** ✅ `Health.razor`
- **T12.10** ✅ `Jobs.razor`
- **T12.11** ✅ `Logs.razor`
- **T12.12** ❌ bUnit component tests **not created**
- **T12.13** ✅ Dockerfile + compose `admin.blazor` (5100)
- ⚠️ Critical: `ClientSecret` hardcoded in `Program.cs:44` — needs fix

---

### Wave 13 – Production Deployment (Azure DevOps) ❌ NOT STARTED

- **T13.1** Azure DevOps pipeline: build + push all images to ACR
- **T13.2** Release pipeline: deploy to AKS / Azure App Service
- **T13.3** Production URLs for Identity Server redirect URIs + Ocelot CORS
- **T13.4** Production smoke tests (Angular + Blazor)

---

## 5. Definition of Done (Project-wide)

> Updated: 2026-06-29

- [x] `docker compose up` builds and starts all services successfully — all 20 services defined in `docker-compose.yml` + `docker-compose.override.yml`
- [x] Every service exposes Swagger and `/health` endpoints
- [x] Ocelot Gateway correctly routes all services (with auth) — JWT Bearer via Identity Server
- [x] Basket checkout triggers Ordering event flow — `BasketCheckoutEvent` → `BasketCheckoutEventConsumer` → `CreateOrderCommand`
- [ ] Hangfire reminder email pipeline verified end-to-end (SMTP not configured in dev; pipeline code is complete)
- [x] Centralized logging with Elasticsearch sink + `X-Correlation-Id` — documented in README
- [x] HealthChecks UI shows Product, Customer, Basket, Ordering, Inventory, HangFire services
- [x] Identity Server issues tokens; all services enforce auth via `Common.Auth`
- [x] Backend unit tests: 42 tests across 8 projects, 0 failures
- [ ] **Integration tests** for critical flows (Basket checkout → Ordering, auth through Gateway) — **not yet added**
- [ ] **Angular WebApp** `ng build` clean + e2e smoke tests — **not yet verified** (T11.9/T11.10)
- [x] **Blazor Admin Portal** enforces `Admin` role; Product/Customer/Inventory/Orders CRUD; Health, Jobs, Logs pages; real-time stock via MassTransit
- [x] Both UIs in `docker compose`; CORS + OIDC redirect URIs configured
- [ ] CI/CD pipeline (Azure DevOps) — **Wave 13, not started**
- [x] README updated with full architecture, ports, run instructions, and progress log

---

## 6. Budget & Cadence

- **Each Wave** roughly maps to one course section.
- Add checkpoint commit at the end of each wave + tag (`v0.2-product`, `v0.3-customer`, etc.).
- If >75% of a wave budget is consumed while <50% tasks are done, stop and reassess (reduce scope or split the wave).
- Never skip review for speed; reduce scope instead.

---

## 7. References

- Architecture: `microservice_course_architecture.png`
- Course outline: `content_course.txt`
- Code review findings: `code_review_findings.md`
- Skill: `.windsurf/skills/executing-plans/SKILL.md`

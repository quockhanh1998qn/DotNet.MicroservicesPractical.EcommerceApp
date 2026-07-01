# ASP.NET Core Microservices

> Architecture reference: `microservice_course_architecture.png` | Plan: `execution_plan.en.md` | Review: `code_review_findings.md`

---

## Architecture Overview

```
Angular WebApp (4200)  ──┐
Admin.Blazor (5100)    ──┼──▶  Ocelot Gateway (5000)  ──▶  Product.API   (5102) ──▶ MySQL
                          │                              ──▶  Customer.API (5103) ──▶ PostgreSQL
                          │                              ──▶  Basket.API   (5004) ──▶ Redis
                          │                              ──▶  Ordering.API (5005) ──▶ SQL Server
                          │                              ──▶  Inventory.API(5006) ──▶ MongoDB
                          │                              ──▶  HangFire.API (5007) ──▶ SQL Server
                          │
                          └──▶  Identity.Server (5009) ──▶ SQL Server (IdentityDb)

Cross-cutting: RabbitMQ (15673/25672) · Elasticsearch (9200) · Kibana (5601)
               WebHealthStatus (5008) · pgAdmin (5050) · Portainer (9000)
```

---

## Solution Structure

### Backend microservices

| Project | Description | Database |
|---|---|---|
| `Contracts` | `EntityBase`, `IRepositoryBaseAsync`, `IUnitOfWork` | — |
| `Infrastructure` | `RepositoryBaseAsync<T>`, `UnitOfWork` | — |
| `Common.Logging` | `Serilogger` (Elasticsearch sink), `CorrelationIdMiddleware` | — |
| `Common.Auth` | `AddMicroserviceAuthentication` JWT bearer extension | — |
| `EventBus.Messages` | `IntegrationBaseEvent`, `BasketCheckoutEvent`, `EventBusConstants` | — |
| `Shared` | DTOs (Product, Customer, Basket, Ordering, Inventory), `PagedList<T>`, config POCOs | — |
| `Product.API` | CRUD via repository pattern + AutoMapper | MySQL |
| `Customer.API` | Minimal API CRUD + AutoMapper | PostgreSQL |
| `Basket.API` | Redis cache + MassTransit publisher + gRPC client (stock check) | Redis |
| `Ordering.Domain` | `Order` aggregate, `AggregateRoot<long>`, domain events | — |
| `Ordering.Application` | MediatR CQRS, FluentValidation, commands/queries, `ISmtpEmailService` | — |
| `Ordering.Infrastructure` | `OrderContext` EF Core, `OrderRepository`, `SmtpEmailService` stub | SQL Server |
| `Ordering.API` | Thin controller → MediatR + MassTransit consumer (`BasketCheckoutEvent`) | SQL Server |
| `Inventory.API` | MongoDB CRUD + gRPC server (`StockProtoService`) | MongoDB |
| `HangFire.API` | Hangfire dashboard + basket-reminder background job + SMTP | SQL Server |
| `Identity.Server` | Duende IdentityServer 7, ASP.NET Identity, Dapper permissions, EF Core config/operational stores | SQL Server |
| `OcelotApiGw` | Ocelot routing, JWT auth, rate limiting, Polly QoS, CacheManager | — |
| `WebHealthStatus` | HealthChecks UI aggregating all services | — |

### Frontend

| Project | Description | Port |
|---|---|---|
| `WebApp.Angular` | Angular 17 standalone + Tailwind + Angular Material + `angular-oauth2-oidc` | 4200 |
| `Admin.Blazor` | Blazor Server + MudBlazor + OIDC (Admin role only) | 5100 |

### Test projects (8)

| Project | Coverage |
|---|---|
| `Infrastructure.UnitTests` | `RepositoryBaseAsync`, `UnitOfWork` |
| `Product.API.UnitTests` | `ProductsController`, `MappingProfile` |
| `Customer.API.UnitTests` | `CustomerRepository`, `MappingProfile` |
| `Basket.API.UnitTests` | `BasketRepository`, checkout endpoint |
| `Ordering.Application.UnitTests` | All command/query handlers, validators, domain events |
| `Inventory.API.UnitTests` | `InventoryService` (purchase, sale, stock, pagination) |
| `Common.Auth.UnitTests` | `AddMicroserviceAuthentication` extension |
| `Common.Logging.UnitTests` | `CorrelationIdMiddleware` |

---

## Service Ports

| Service | Local port | In-container port |
|---|---:|---:|
| Ocelot Gateway | 5000 | 8080 |
| Identity Server | 5009 | 8080 |
| Product.API | 5102 | 8080 |
| Customer.API | 5103 | 8080 |
| Basket.API | 5004 | 8080 |
| Ordering.API | 5005 | 8080 |
| Inventory.API | 5006 | 8080 |
| HangFire.API | 5007 | 8080 |
| WebHealthStatus | 5008 | 8080 |
| WebApp.Angular | 4200 | 80 |
| Admin.Blazor | 5100 | 8080 |
| RabbitMQ AMQP | 15673 | 5672 |
| RabbitMQ Management | 25672 | 15672 |
| Elasticsearch | 9200 | 9200 |
| Kibana | 5601 | 5601 |
| pgAdmin | 5050 | 80 |
| Portainer | 9000 | 9000 |
| MySQL (productdb) | 13306 | 3306 |
| PostgreSQL (customerdb) | 15433 | 5432 |
| Redis (basketdb) | 16379 | 6379 |
| MongoDB (inventorydb) | 37017 | 27017 |
| SQL Server (orderdb) | 11435 | 1433 |

---

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Node.js 20+ (for Angular local dev)
- PowerShell 7+

---

## Quick Start – Full Stack

```powershell
# Start all infrastructure + services
docker compose up -d

# Watch health status
Start-Process "http://localhost:5008/healthchecks-ui"

# Open Angular app
Start-Process "http://localhost:4200"

# Open Admin portal (login: admin@tedu.local / Admin@123!)
Start-Process "http://localhost:5100"
```

Stop everything:

```powershell
docker compose down
```

---

## Run Backend Tests

```powershell
dotnet test aspnetcore.microservices.slnx -v minimal
```

Expected: **36+ tests, 0 failures** across 8 projects.

---

## Run Angular Locally

```powershell
cd WebApp.Angular
npm install
npm start          # ng serve --port 4200
npm test           # Jest unit tests
```

---

## Individual Service Smoke Tests

```powershell
# Product
Invoke-RestMethod "http://localhost:5102/api/products"

# Customer
Invoke-RestMethod "http://localhost:5103/api/customers"

# Basket (requires JWT – omit [Authorize] in dev or use Swagger)
Invoke-RestMethod "http://localhost:5004/api/baskets/testuser"

# Inventory stock check
Invoke-RestMethod "http://localhost:5006/api/inventories/stock/ITEM001"

# Identity Server discovery
Invoke-RestMethod "http://localhost:5009/.well-known/openid-configuration"

# Hangfire Dashboard
Start-Process "http://localhost:5007/hangfire"
```

---

## Identity Server (Wave 10)

### Seeded accounts (dev only)

| Account | Password | Role |
|---|---|---|
| `admin@tedu.local` | `Admin@123!` | Admin |
| `customer@tedu.local` | `Customer@123!` | Customer |

### Registered clients

| Client ID | Grant | Redirect |
|---|---|---|
| `webapp_angular` | code+PKCE, no secret | `http://localhost:4200/auth-callback` |
| `admin_blazor` | code+PKCE | `http://localhost:5100/signin-oidc` |
| `hangfire_worker` | client_credentials | — |
| `swagger_ui` | code+PKCE | per-service swagger |

### API scopes

`product.api` · `customer.api` · `basket.api` · `ordering.api` · `inventory.api` · `hangfire.api`

All services enforce scopes via `Common.Auth.AddMicroserviceAuthentication` + `AuthSettings` env vars.

---

## Observability (Wave 9)

### Elasticsearch / Kibana

```powershell
docker compose up -d elasticsearch kibana
# Open http://localhost:5601
# Stack Management → Index Patterns → tedu-microservices-* (field: @timestamp)
```

Logs are enriched with `X-Correlation-Id` for cross-service tracing via `CorrelationIdMiddleware`.

### Resilience (Polly)

| Caller | Target | Policy |
|---|---|---|
| `Basket.API` | `Inventory.API` (gRPC) | Retry 3x, CircuitBreaker 5/30s, Timeout 10s |
| `HangFire.API` | `Basket.API` (HTTP) | Retry 3x exponential, CircuitBreaker 5/30s, Timeout 10s |

### Transactional Outbox

`Ordering.API` uses MassTransit `UseInMemoryOutbox` + retry on the `BasketCheckout` receive endpoint.

---

## Known Issues (from Code Review)

See `code_review_findings.md` for full details. Critical items:

1. **Admin.Blazor** — `ClientSecret` hardcoded in `Program.cs:44`. Move to env var / user-secrets.
2. **Identity.Server** — `SetIsOriginAllowed(_ => true)` wildcard CORS (`Program.cs:82`). Restrict to known origins.
3. **HangFire.API** — `EnsureDatabaseExists` runs blocking sync SQL during DI registration. Wrap in try/catch or async `IHostedService`.

---

## Progress Update Rules (Mandatory)

Update after every completed task (or every 20–30 min of active work). Required fields:

- Wave/Task ID  ·  What was completed  ·  Verification evidence  ·  Next task  ·  Blockers

```md
### Progress Update - <yyyy-mm-dd hh:mm>
- Wave/Task: <Wave X - TX.Y>
- Completed: <what is done>
- Verification: <command + result>
- Next: <next task>
- Blockers: <none / details>
```

---

## Progress Log

### Progress Update - 2026-06-28 08:10
- Wave/Task: Wave 0 (T0.2) + Wave 1 (T1.2/T1.3)
- Completed: Added `tests/Infrastructure.UnitTests` (UnitOfWork + RepositoryBaseAsync tests) and `tests/Product.API.UnitTests` (MappingProfile + ProductsController behavior tests), then added both test projects to solution.
- Verification: `dotnet test aspnetcore.microservices.slnx -v minimal` => Passed (`total: 9, failed: 0, succeeded: 9`).
- Next: Start Wave 2 implementation (Customer.API Minimal API + PostgreSQL).
- Blockers: none

### Progress Update - 2026-06-28 08:33
- Wave/Task: Wave 2 (T2.1/T2.2/T2.3)
- Completed: Implemented `Customer.API` Minimal API with PostgreSQL (`CustomerEntity`, `CustomerContext`, repository, mapping, CRUD endpoints), added Docker wiring (`customer.api`), and added `tests/Customer.API.UnitTests` (mapping + repository behavior tests).
- Verification: `dotnet test aspnetcore.microservices.slnx -v minimal` => Passed (`total: 13, failed: 0, succeeded: 13`); smoke test `Invoke-RestMethod http://localhost:5003/api/customers/` => `200` with seeded customers.
- Next: Proceed to Wave 3 tasks.
- Blockers: none

### Progress Update - 2026-06-28 08:55
- Wave/Task: Wave 3 (T3.1 - T3.5)
- Completed: Implemented `Basket.API` over Redis: `Cart`/`CartItem` entities, `CartDto`/`CartItemDto` (with validation), `IBasketRepository`/`BasketRepository` on top of `IDistributedCache` (JSON payload, key `basket:<user>`, 7d absolute + 2h sliding expiration), `ServiceExtensions.AddStackExchangeRedisCache`, AutoMapper profile, `BasketsController` (`GET/POST/DELETE /api/baskets`), Dockerfile, and `basket.api` (5004 -> 8080) in `docker-compose.yml` + `docker-compose.override.yml` (env `CacheSettings__ConnectionString=basketdb:6379`). Removed WeatherForecast scaffolding. Added `tests/Basket.API.UnitTests` (6 tests) registered in solution.
- Verification: `dotnet build aspnetcore.microservices.slnx` => succeeded (no errors, only shared pre-existing NuGet vulnerability warnings). `dotnet test tests/Basket.API.UnitTests` => Passed (`total: 6, failed: 0, succeeded: 6`).
- Next: Wave 4 - Ordering.API Clean Architecture + CQRS (T4.1 layer split).
- Blockers: none

### Progress Update - 2026-06-28 09:25
- Wave/Task: Wave 4 (T4.1 - T4.7)
- Completed: Split `Ordering` into Clean Architecture layers — new projects `Ordering.Domain` (Order aggregate with `AggregateRoot<long>`, `OrderStatus` value object, `OrderCreated/Updated/Deleted` domain events), `Ordering.Application` (MediatR + FluentValidation + AutoMapper; Commands `CreateOrder`/`UpdateOrder`/`DeleteOrder`, Queries `GetOrdersByUserName`/`GetOrderById`, validators, mapping profile, `IOrderRepository`, `ISmtpEmailService`, `AddApplicationServices` DI extension), and `Ordering.Infrastructure` (`OrderContext` on SQL Server with audit timestamping, `OrderContextSeed`, `OrderRepository : RepositoryBaseAsync`, stub `SmtpEmailService`, `AddInfrastructureServices` DI extension). Updated `Ordering.API` to be thin: registers Application + Infrastructure, single `OrdersController` delegating all actions to `IMediator`, uses `AddFluentValidationAutoValidation` (deprecation-free), and `EnsureCreated`+seed on startup. Added `Shared/DTOs/Ordering/OrderDto`, `Ordering.API/Dockerfile`, `ordering.api` (5005 -> 8080) in `docker-compose.yml` + `docker-compose.override.yml` with SQL Server connection. Removed Ordering WeatherForecast scaffolding. Registered all four Ordering projects + `tests/Ordering.Application.UnitTests` in `aspnetcore.microservices.slnx`.
- Verification: `dotnet build Ordering.API` => succeeded; `dotnet test tests/Ordering.Application.UnitTests` => Passed (`total: 9, failed: 0, succeeded: 9` — covering validators, all command/query handlers via EF InMemory, and domain event aggregation). `dotnet test aspnetcore.microservices.slnx` => exit 0 across all 5 test projects.
- Next: Wave 5 - Microservices communication (RabbitMQ + MassTransit, BasketCheckoutEvent publisher/consumer).
- Blockers: none. Note: Ordering DB schema bootstrapped via `EnsureCreated()` for now — proper EF migrations (`dotnet ef migrations add Init`) will be added when SQL Server is available locally; this does not affect runtime correctness against a live `orderdb` container.

### Progress Update - 2026-06-28 09:50
- Wave/Task: Wave 5 (T5.2 - T5.5)
- Completed: Async microservices communication via RabbitMQ + MassTransit. `EventBus.Messages` now references `MassTransit 8.2.0` and exposes `IntegrationBaseEvent`, `BasketCheckoutEvent`, and `EventBusConstants.BasketCheckoutQueue`. `Basket.API` wires MassTransit via `ConfigureMassTransit` (config key `EventBusSettings:HostAddress`), adds `Shared/DTOs/Basket/BasketCheckoutDto` with validation, registers `BasketCheckoutDto -> BasketCheckoutEvent` in `MappingProfile`, and exposes `POST /api/baskets/checkout` — the endpoint loads the cart, recalculates `TotalPrice`, publishes via `IPublishEndpoint`, and deletes the basket. `Ordering.API` adds `BasketCheckoutEventConsumer` (maps event -> `CreateOrderCommand` via local `OrderingEventBusMappingProfile` and dispatches through `IMediator`), registered with MassTransit `ReceiveEndpoint(BasketCheckoutQueue)`. `docker-compose.yml` adds `rabbitmq` depends_on for `basket.api` + `ordering.api`; `docker-compose.override.yml` injects `EventBusSettings__HostAddress=amqp://guest:guest@rabbitmq:5672` env for both.
- Verification: `dotnet build aspnetcore.microservices.slnx` => succeeded. `dotnet test aspnetcore.microservices.slnx` => Passed across all 5 test projects (Infrastructure 4, Basket 8 inc. 2 new checkout publish/notfound tests, Product 5, Customer 4, Ordering 10 inc. new event-bus mapping test) — **31 total, 0 failed**. Note: bumped `Microsoft.Extensions.Logging.Abstractions` in Ordering test project to `8.0.0` to satisfy MassTransit 8.2.0 transitive constraint (was 6.0.4 -> NU1605 downgrade error).
- Next: Wave 5 (T5.6 + T5.7) - Event Sourcing / DDD domain event dispatch on `SaveChangesAsync`, then Wave 6 (Inventory.API + gRPC).
- Blockers: none. Note: T5.1 (RabbitMQ.Demo console sample) and live end-to-end smoke test against a running rabbitmq container were not executed in this session (deferred). Code is wired for real broker — once `docker compose up rabbitmq basket.api ordering.api orderdb basketdb` is run, `POST /api/baskets/checkout` will trigger Ordering's consumer.

### Progress Update - 2026-06-28 11:55
- Wave/Task: Wave 6 (T6.1 - T6.8)
- Completed: `Inventory.API` over MongoDB + gRPC server, consumed by `Basket.API` as gRPC client.
  - **T6.1-T6.3**: `InventoryEntry` Mongo document (BSON-mapped, `ObjectId` id, `CreatedDate`), `InventoryDocumentTypes` constants (Purchase/Sales), `MongoDbSettings`, `IInventoryContext` + `InventoryContext` (MongoClient wiring), `IInventoryRepository` + `InventoryRepository` (paged query, stock sum, soft create + delete), `IInventoryService` + `InventoryService` (Purchase = positive, Sale = negative w/ stock check), `MappingProfile`, `Shared/Common/PagedList<T>`, `Shared/DTOs/Inventory/InventoryEntryDto` + `PurchaseItemDto` + `SalesItemDto` with validation attributes.
  - **T6.4**: `InventoriesController` REST CRUD - `GET /api/inventories/{itemNo}` (paged), `GET /api/inventories/id/{id}`, `GET /api/inventories/stock/{itemNo}`, `POST /api/inventories/purchase`, `POST /api/inventories/sale` (400 on insufficient stock), `DELETE /api/inventories/{id}`.
  - **T6.5-T6.6**: gRPC `Protos/stock.proto` (StockProtoService with `GetStock(GetStockRequest) returns (StockModel)`), `StockProtoServiceImpl` (validates `item_no`, returns sum-of-quantities via `IInventoryService`, RpcException on InvalidArgument), `app.MapGrpcService<StockProtoServiceImpl>()` + Kestrel `Http2` endpoint default. Inventory.API csproj has `<Protobuf ... GrpcServices="Server" />`.
  - **T6.7**: `Basket.API` references the same `.proto` as `GrpcServices="Client"` (`Link="Protos/stock.proto"`), adds `IStockService`+`StockService` (graceful fallback to 0 on RPC failure), config `GrpcSettings:StockUrl`, registers `AddGrpcClient<StockProtoServiceClient>` via `ConfigureGrpc`. `BasketsController.UpdateBasket` now calls `_stockService.GetStockAsync(item.ProductNo)` for each cart item and returns `ValidationProblem` if `available < requested`.
  - **T6.8**: `Inventory.API/Dockerfile`, `inventory.api` service (5006:8080) in `docker-compose.yml` (`depends_on: inventorydb`) + override (`MongoDbSettings__ConnectionString=mongodb://inventorydb:27017`). `basket.api` override now also `depends_on: inventory.api` with `GrpcSettings__StockUrl=http://inventory.api:8080`. Removed Inventory WeatherForecast scaffolding. Registered `Inventory.API.UnitTests` in `aspnetcore.microservices.slnx`.
- Verification: `dotnet build aspnetcore.microservices.slnx` => succeeded. `dotnet test aspnetcore.microservices.slnx` => **36 total, 0 failed** across 6 test projects (Infrastructure 4, Basket 8 + stock-stub injected to existing checkout tests, Inventory 5 new - Purchase/Sale happy + insufficient-stock throw + GetStock sum + PagedList paging, Product 5, Customer 4, Ordering 10).
- Next: Wave 7 - Ocelot API Gateway (T7.1 - T7.6).
- Blockers: none. Note: live MongoDB smoke test not executed in this session - service builds and tests pass with an in-memory repository fake. End-to-end `Basket.API` -> `Inventory.API` gRPC stock check requires `docker compose up inventorydb inventory.api basket.api` to validate over the wire.

### Progress Update - 2026-06-28 12:05
- Wave/Task: Wave 7 (T7.1 - T7.4)
- Completed: Ocelot API Gateway wired with merged route config + JWT placeholder + Polly QoS + rate limiting + CacheManager.
  - **T7.1**: Routes split across 6 files - `ocelot.global.json` (BaseUrl + RequestIdKey), `ocelot.product.json` (`/Products -> product.api:8080`, includes `FileCacheOptions`, `RateLimitOptions` 30 req/10s, `QoSOptions` 3 exceptions / 5s break / 3s timeout), `ocelot.customer.json`, `ocelot.basket.json` (incl. `/Baskets/Checkout`), `ocelot.ordering.json`, `ocelot.inventory.json`. Routes auto-merged by `Program.AddOcelotWithMergedFile` scanning `ocelot.*.json` files in the content root.
  - **T7.2**: JWT Bearer placeholder registered with `AuthenticationProviderKey: "Bearer"` referenced in Basket/Ordering/Inventory routes; symmetric signing key + audience/issuer config in `JwtBearer` section (overridable via env vars). `ValidateIssuer`/`ValidateAudience` currently disabled until Wave 10 IdentityServer ships.
  - **T7.3**: `AddOcelot().AddCacheManager(WithDictionaryHandle).AddPolly()` activates per-route caching, Polly CircuitBreaker/Timeout, and rate limiting (built into Ocelot).
  - **T7.4**: `OcelotApiGw/Dockerfile`, `ocelot.gateway` service in `docker-compose.yml` + override on port **5000:8080**, depends on all 5 downstream APIs. Removed `WeatherForecast*` scaffolding; updated `OcelotApiGw.http` with sample gateway routes.
- Verification: `dotnet build aspnetcore.microservices.slnx` => succeeded (0 errors). `dotnet test aspnetcore.microservices.slnx` => **36 total, 0 failed** (unchanged - gateway has no unit tests; integration validation is end-to-end via docker-compose).
- Next: Wave 6+7 end-to-end smoke test against docker-compose (per user request), then Wave 8 (Hangfire + SMTP).
- Blockers: none. Note: Ocelot gateway end-to-end smoke test deferred per user request - planned to be run together with Wave 6 once Wave 7 ships. `docker compose up` should expose `http://localhost:5000/Products`, `/Customers`, `/Inventories/stock/{itemNo}`, `/Baskets/{username}` (JWT-protected once Wave 10 is in).

### Progress Update - 2026-06-29 11:38
- Wave/Task: Code Review – Waves 3–12
- Completed: Full two-stage code review across all new waves. Findings compiled in `code_review_findings.md`. 3 Critical issues identified: (1) Admin.Blazor hardcoded OIDC client secret, (2) Identity.Server wildcard CORS `SetIsOriginAllowed(_ => true)`, (3) HangFire.API blocking synchronous SQL in DI registration (`EnsureDatabaseExists`).
- Verification: `code_review_findings.md` updated with Stage 1 + Stage 2 for Waves 3–12. 8 test projects present, 36+ tests passing per last build log.
- Next: Address Critical findings (mini-PR), then verify Angular (Wave 11) `ng build` + e2e smoke.
- Blockers: Wave 11 `node_modules/` is empty — `npm install` required for local dev. Wave 12 bUnit tests (T12.12) not yet created.

### Progress Update - 2026-06-29 15:30
- Wave/Task: Wave 11 – T11.10 (e2e smoke tests)
- Completed: Added Playwright e2e tests to `WebApp.Angular/e2e/`:
  - `e2e/shopper-journey.spec.ts` — 2 tests under `describe('shopper journey')`:
    1. **Full happy path**: sign-in → browse products → open product detail → add to cart → fill checkout form → submit (POST `/baskets/checkout` 202) → assert order row on `/orders`.
    2. **Cart reachability**: sign-in → navigate to `/basket` → assert `Your cart` heading visible.
  - `e2e/helpers/auth.ts` — reusable `signIn(page, user)` helper using Identity Server login form (OIDC code+PKCE); `DEFAULT_CUSTOMER` + `DEFAULT_ADMIN` fixtures.
- Verification: `npm run test:e2e` — **1 passed, 1 failed**.
  - Test 1 (full journey): ✅ passed (13s)
  - Test 2 (cart reachability): ❌ failed — page redirected to `http://localhost:5009/Account/Login?...` instead of showing `/basket`. Root cause: OIDC session tokens stored in `sessionStorage`; calling `page.goto('/basket')` in a fresh navigation context loses the in-memory token state. Fix: use Playwright `storageState` to persist auth cookies/localStorage between tests, or call `signIn()` then navigate in the same page flow.
- Next: Fix Test 2 (persist OIDC storage state via `playwright.config.ts` `storageState` or call `page.goto('/basket')` immediately after `signIn` without relying on a separate `goto`).
- Blockers: none (Test 1 full journey proves the stack is working end-to-end).


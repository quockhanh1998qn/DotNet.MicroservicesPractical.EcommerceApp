# Execution Plan – ASP.NET Core Microservices

> **Iron Law:** Follow the plan. Review every task. Never trust self-reports.
>
> The plan is the spec. If the plan is wrong, update the plan first — then execute.

---

## 1. Mục tiêu (Objective)

Xây dựng một hệ thống Microservices hoàn chỉnh theo kiến trúc trong `microservice_course_architecture.png`:

- **API Gateway** (Ocelot, port 5000) định tuyến tới các microservices
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
  - **WebApp (Angular 17+)** – SPA cho end-user (port 4200): duyệt sản phẩm, giỏ hàng, đặt hàng, xem đơn, login qua Identity Server (OIDC + PKCE)
  - **Admin Portal (Blazor Server)** – Giao diện quản trị (port 5100): quản lý Product / Customer / Inventory / Orders, xem health status, xem Hangfire jobs

---

## 2. Tình trạng hiện tại (Current State Snapshot)

| Component | Status | Ghi chú |
|---|---|---|
| `Product.API` | Đang phát triển | Có `CatalogProduct` entity, `ProductContext`, `ProductRepository`, `ProductsController` CRUD cơ bản, Serilog, MigrateDatabase + Seed |
| `Customer.API` | Skeleton | Cần entity, DTOs, repo, CRUD, PostgreSQL |
| `Basket.API` | Skeleton | Cần Redis integration + RabbitMQ |
| `Ordering.API` | Skeleton | Cần Clean Architecture + CQRS + EF Core |
| `Inventory.API` | Skeleton | Cần MongoDB + gRPC |
| `HangFire.API` | Skeleton | Cần Hangfire + email |
| `OcelotApiGw` | Skeleton | Cần ocelot.json + Auth + RateLimit + QoS |
| `WebHealthStatus` | Skeleton | Cần aggregate healthchecks UI |
| `WebApp.Angular` | Chưa có | Sẽ tạo mới ở Wave 12 |
| `Admin.Blazor` | Chưa có | Sẽ tạo mới ở Wave 13 |
| `EventBus.Messages` | Tồn tại | Cần định nghĩa Integration Events |
| `Common.Logging` | Tồn tại (`Serilogger`) | OK |
| `Contracts` | Có `EntityBase`, `EntityAuditBase`, `IRepositoryBaseAsync`… | OK |
| `Infrastructure` | Có `RepositoryBaseAsync`, `UnitOfWork` | OK |
| `Shared` | Có `DTOs/Product` | Sẽ mở rộng |
| `docker-compose` | Có productdb (mysql), customerdb (postgres), basketdb (redis), inventorydb (mongo), orderdb (sqlserver), rabbitmq, elastic, kibana, pgadmin, portainer | Còn thiếu identity DB, services tự build |

---

## 3. Nguyên tắc thực thi (Execution Principles)

1. **Mỗi task** = một đơn vị nhỏ, có thể review độc lập (≤ 30 phút code).
2. **Two-stage review** mỗi task:
   - **Stage 1 – Spec compliance:** đúng yêu cầu trong plan?
   - **Stage 2 – Code quality:** clean, tests pass, no warnings, conventions tuân thủ.
3. **Checkpoint** mỗi 3–5 task: chạy `dotnet build`, smoke test, commit.
4. **Wave** = nhóm task có thể parallel (không share file). Task trong cùng wave có thể chạy song song; **không** parallel trên cùng file.
5. **Verification trước khi đóng task:**
   - `dotnet build` clean
   - Endpoint hoạt động qua Swagger / `.http`
   - Container (nếu có) `docker compose up` thành công
6. Nếu **stuck 3 lần** trên cùng task → dừng, update plan, rồi tiếp tục.
7. **Bắt buộc test coverage theo mức độ phù hợp:**
   - Backend: unit test cho business logic (Domain/Application/Service), không chỉ smoke test API.
   - API/Infrastructure: integration test cho luồng chính (DB, event bus, auth).
   - UI: unit/component tests + e2e smoke test cho critical user journeys.

---

## 4. Waves & Tasks

### Wave 0 – Hoàn thiện nền tảng dùng chung

> Mục tiêu: chuẩn hoá base trước khi quay lại Product.API và các service khác.

- **T0.1** Audit `Contracts` (EntityBase, EntityAuditBase, IRepositoryBaseAsync, IUnitOfWork). Bổ sung interface còn thiếu.
- **T0.2** Audit `Infrastructure.Common.RepositoryBaseAsync` + `UnitOfWork`. Đảm bảo `FindAll`, `FindByCondition`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `SaveChangesAsync` đầy đủ.
- **T0.3** Audit `Common.Logging.Serilogger` – cấu hình ghi log file + console; chuẩn bị sink Elasticsearch (sẽ enable ở Section 10).
- **T0.4** Tạo `Shared/Configurations` cho `DatabaseSettings`, `EmailSettings`, v.v. (POCO dùng chung).

**Exit criteria:** `dotnet build` toàn solution clean, không warning ở project nền tảng.

---

### Wave 1 – Section 2: Hoàn thiện Product.API + Containerize

- **T1.1** Hoàn thiện DTOs `Shared/DTOs/Product` (`ProductDto`, `CreateProductDto`, `UpdateProductDto`).
- **T1.2** Cập nhật `MappingProfile.cs` cho CRUD DTO ↔ Entity.
- **T1.3** Refactor `ProductsController`:
  - `CreateProduct` nhận `CreateProductDto` (không nhận entity)
  - `UpdateProduct` nhận `UpdateProductDto`
  - Trả `ProductDto`
  - Validation attributes + `[ProducesResponseType]`
- **T1.4** Bổ sung `IProductRepository` interface đầy đủ + DI registration trong `ServiceExtensions`.
- **T1.5** Kiểm tra `ProductContext` + `Migrations` + `ProductContextSeed` chạy đúng trên MySQL.
- **T1.6** Cấu hình `appsettings.json` connection MySQL + chạy local thành công.
- **T1.7** Tạo `Dockerfile` cho Product.API; cập nhật `docker-compose.yml` & `docker-compose.override.yml` thêm service `product.api` (port 5002) phụ thuộc `productdb`.
- **T1.8** `docker compose up product.api productdb` chạy → smoke test `GET /api/products`.

**Checkpoint:** commit `feat(product-api): complete CRUD + containerize`.

---

### Wave 2 – Section 3: Customer.API (Minimal API + PostgreSQL)

- **T2.1** Khởi tạo project `Customer.API` (Minimal API), thêm vào solution.
- **T2.2** Tạo entity `Customer`, DTOs (`CustomerDto`, `CreateCustomerDto`, `UpdateCustomerDto`) trong `Shared/DTOs/Customer`.
- **T2.3** `CustomerContext` (EF Core + Npgsql), migrations, seeding.
- **T2.4** Repository Pattern (`ICustomerRepository` + `CustomerRepository`).
- **T2.5** Minimal API endpoints CRUD trong `Program.cs` (map group `/api/customers`).
- **T2.6** ServiceExtensions, AutoMapper profile.
- **T2.7** Dockerfile + compose service `customer.api` (5003) phụ thuộc `customerdb`.
- **T2.8** Smoke test qua Swagger / `.http`.

**Checkpoint + Livecode tổng kết Section 1-2-3** (commit, demo).

---

### Wave 3 – Section 4: Basket.API + Redis

- **T3.1** Entity `Cart`, `CartItem`; DTOs cho Basket.
- **T3.2** `IBasketRepository` + `BasketRepository` dùng `IDistributedCache` (Redis).
- **T3.3** Cấu hình `StackExchange.Redis` + `AddStackExchangeRedisCache` trong ServiceExtensions.
- **T3.4** Endpoints `GET /api/baskets/{username}`, `POST /api/baskets`, `DELETE /api/baskets/{username}`.
- **T3.5** Dockerfile + compose `basket.api` (5004) + `basketdb`.

---

### Wave 4 – Section 5: Ordering.API – Clean Architecture + CQRS

> Layer tách project: `Ordering.Domain`, `Ordering.Application`, `Ordering.Infrastructure`, `Ordering.API`.

- **T4.1** Tạo 4 project layer + reference đúng theo Clean Architecture.
- **T4.2** Domain: `Order` aggregate, ValueObjects, DomainEvents.
- **T4.3** Application: MediatR + AutoMapper + FluentValidation.
  - Commands: `CreateOrder`, `UpdateOrder`, `DeleteOrder`.
  - Queries: `GetOrdersByUserName`, `GetOrderById`.
- **T4.4** Infrastructure: `OrderContext` (SQL Server), repositories, migrations, seeding.
- **T4.5** Email Service (Google SMTP) – interface `ISmtpEmailService`.
- **T4.6** API controller mỏng, gọi MediatR.
- **T4.7** Dockerfile + compose `ordering.api` (5005) + `orderdb`.

---

### Wave 5 – Section 6: Microservices Communication (RabbitMQ + MassTransit)

- **T5.1** Console app demo RabbitMQ (riêng folder `Samples/RabbitMQ.Demo`).
- **T5.2** `EventBus.Messages` – định nghĩa `BasketCheckoutEvent`, base `IntegrationBaseEvent`.
- **T5.3** Basket.API – publish `BasketCheckoutEvent` khi checkout (MassTransit + RabbitMQ).
- **T5.4** Ordering.API – consumer nhận `BasketCheckoutEvent`, tạo Order qua MediatR.
- **T5.5** Update docker-compose: dependency `rabbitmq` cho Basket + Ordering.
- **T5.6** Event Sourcing with DDD – Part I (apply domain events trong Ordering).
- **T5.7** Event Sourcing with DDD – Part II (persist + dispatch).

---

### Wave 6 – Section 7: Inventory.API + MongoDB + gRPC

- **T6.1** Khởi tạo `Inventory.API` + MongoDB driver.
- **T6.2** Entity `InventoryEntry` + DTOs + repository (`IInventoryRepository`).
- **T6.3** Service layer + abstraction pagination (`PagedList<T>`).
- **T6.4** REST API CRUD inventory.
- **T6.5** gRPC project `Inventory.Grpc` (Part I): `.proto`, service implementation.
- **T6.6** gRPC Part II: stock query (`GetStock`), error handling.
- **T6.7** Basket.API consume gRPC `GetStock` trước khi add item.
- **T6.8** Dockerfile + compose `inventory.api` (5006) + `inventorydb`.

---

### Wave 7 – Section 8: API Gateway (Ocelot)

- **T7.1** Cấu hình `ocelot.json` route tới tất cả service downstream.
- **T7.2** Authentication (JWT Bearer) — placeholder cho IdentityServer (Wave 11).
- **T7.3** Rate Limiting + QoS (Polly bên trong Ocelot) + Response Caching.
- **T7.4** Dockerfile + compose `ocelot.gateway` (5000); test routing end-to-end.

---

### Wave 8 – Section 9: Hangfire Background Job

- **T8.1** Tích hợp Hangfire (SQL Server storage) vào `HangFire.API` (5007).
- **T8.2** Dashboard `/hangfire`.
- **T8.3** Job: gửi email nhắc nếu customer chưa checkout sau X phút.
- **T8.4** Dockerfile + compose.

---

### Wave 9 – Section 10: Observability + Resilience + HealthChecks

- **T9.1** Tích hợp Serilog → Elasticsearch sink trong `Common.Logging`.
- **T9.2** Kibana dashboard cơ bản (manual setup, document trong README).
- **T9.3** Correlation ID middleware – log request giữa các services.
- **T9.4** Thư viện `Polly` – wrap `HttpClient` calls (Retry, CircuitBreaker, Timeout, Bulkhead, Fallback, Cache).
- **T9.5** Áp dụng Polly cho Basket → Inventory gRPC + các HTTP call qua Gateway.
- **T9.6** Cài `AspNetCore.HealthChecks.*` cho từng service (DB + dependencies).
- **T9.7** `WebHealthStatus` – HealthChecks UI aggregate, dashboard `/healthchecks-ui`.
- **T9.8** Transaction management strategy (Saga / Outbox) – document + implement Outbox cho Ordering.

---

### Wave 10 – Section 11 & 12: Identity Server + Auth toàn hệ thống

- **T10.1** Tạo `Identity.Server` với template Duende.
- **T10.2** Cấu hình Serilog + Scopes + ApiResources + Clients.
- **T10.3** Migrations: Config DB + Persisted Grant DB.
- **T10.4** .NET Core Identity (Users, Roles).
- **T10.5** Authentication + SMTP email (confirm, reset password).
- **T10.6** Repository Pattern + Repository Manager (Lazy Loading).
- **T10.7** Permission entity + Dapper + Stored Procedures (Part I & II).
- **T10.8** Bearer policy + Permission list endpoints.
- **T10.9** Áp dụng Authentication & Authorization cho Product.API (Part I & II).
- **T10.10** Áp dụng cho các services còn lại.
- **T10.11** Cấu hình JWT cho Ocelot Gateway.
- **T10.12** Containerize Identity Service + cấu hình A&A (Part I & II).

---

### Wave 11 – Client WebApp (Angular)

> Mục tiêu: SPA end-user gọi vào Ocelot Gateway, login qua Identity Server.

- **T11.1** Khởi tạo project `WebApp.Angular` (Angular 17+, standalone components, signals). Cấu trúc folder: `core/`, `shared/`, `features/{product,basket,order,auth}`.
- **T11.2** Cấu hình Tailwind CSS + Angular Material (hoặc PrimeNG) cho UI components.
- **T11.3** `ApiClient` services (HttpClient) cho Product, Basket, Order — base URL trỏ vào Ocelot Gateway (`http://localhost:5000`).
- **T11.4** Tích hợp `angular-oauth2-oidc` (OIDC + PKCE) với Identity Server; HTTP interceptor đính `Authorization: Bearer`.
- **T11.5** Feature **Product**: list (pagination, search), detail page.
- **T11.6** Feature **Basket**: add to cart, view cart, update quantity, checkout (gọi `POST /basket/checkout`).
- **T11.7** Feature **Order**: list orders của user hiện tại, order detail.
- **T11.8** Auth guards + role-based route protection.
- **T11.9** Error handling + loading states + toast notifications.
- **T11.10** Unit tests (Jest hoặc Karma) cho services & key components; e2e smoke (Playwright/Cypress) cho luồng login → add to cart → checkout.
- **T11.11** `Dockerfile` (multi-stage: build Angular → serve qua nginx). Cập nhật `docker-compose` service `webapp.angular` (port 4200 → 80).
- **T11.12** Cập nhật Ocelot CORS + Identity Server allowed redirect URIs cho `http://localhost:4200`.

**Exit criteria:** Luồng end-to-end (login → browse → add to cart → checkout → order created → email reminder qua Hangfire) chạy được từ Angular UI.

---

### Wave 12 – Admin Portal (Blazor Server)

> Mục tiêu: portal nội bộ cho admin, demo sức mạnh Blazor Server (real-time qua SignalR, ít JS).

- **T12.1** Khởi tạo project `Admin.Blazor` (Blazor Server, .NET 8, InteractiveServer render mode).
- **T12.2** Layout + navigation: MudBlazor (hoặc Radzen) — sidebar, top bar, theme dark/light.
- **T12.3** Authentication qua Identity Server (OIDC code flow) — chỉ cho phép role `Admin`.
- **T12.4** Typed `HttpClient` cho mỗi microservice (qua Ocelot) + `DelegatingHandler` đính bearer token.
- **T12.5** Trang **Products Management**: CRUD đầy đủ (data grid, form, validation).
- **T12.6** Trang **Customers Management**: CRUD + view chi tiết.
- **T12.7** Trang **Inventory Management**: search + adjust stock; hiển thị stock realtime qua SignalR (push từ Inventory.API khi stock thay đổi — phát qua RabbitMQ → Blazor hub consumer).
- **T12.8** Trang **Orders Dashboard**: list orders, filter theo status, drill-down detail.
- **T12.9** Trang **System Health**: nhúng `WebHealthStatus` UI hoặc gọi `/health` của từng service.
- **T12.10** Trang **Hangfire Jobs**: iframe / link tới `/hangfire` dashboard (đã auth).
- **T12.11** Trang **Logs**: link nhúng tới Kibana dashboard.
- **T12.12** Component tests (bUnit) cho các form quan trọng.
- **T12.13** Dockerfile + compose service `admin.blazor` (port 5100). Cập nhật Identity Server clients.

**Exit criteria:** Admin login → CRUD ít nhất Product + Inventory → thấy stock update realtime → xem health/logs/jobs.

---

### Wave 13 – Deploy Production (Section 12 phần Azure DevOps)

- **T13.1** Azure DevOps pipeline – build + push image lên ACR (tất cả services + Angular + Blazor).
- **T13.2** Release pipeline – deploy lên AKS hoặc Azure App Service (Angular qua Static Web Apps / nginx container; Blazor Server qua App Service).
- **T13.3** Cấu hình production URLs cho Identity Server redirect + Ocelot CORS.
- **T13.4** Smoke test môi trường production (cả Angular + Blazor).

---

## 5. Definition of Done (toàn dự án)

- [ ] `docker compose up` build & start toàn bộ services không lỗi.
- [ ] Mọi service expose Swagger + healthcheck.
- [ ] Ocelot Gateway route đúng mọi service (kèm auth).
- [ ] Basket checkout → Ordering nhận event → email gửi qua Hangfire job.
- [ ] Logs tập trung tại Kibana, có correlation ID.
- [ ] HealthChecks UI hiển thị tất cả service.
- [ ] Identity Server cấp token; mọi service enforce auth.
- [ ] Backend có **unit tests đầy đủ cho các use case cốt lõi** (Product, Customer, Basket, Ordering, Inventory, Identity) và test chạy ổn định.
- [ ] Có **integration tests cho các luồng quan trọng**: Product CRUD, Basket checkout → Ordering consumer, và authentication flow qua Gateway.
- [ ] **Angular WebApp** chạy được luồng end-to-end: login (Identity Server) → browse Product → add Basket → checkout → nhận order qua Ordering.
- [ ] **Blazor Admin Portal** truy cập bằng role `Admin`, CRUD Product/Customer/Inventory, xem realtime stock update, xem health + jobs + logs.
- [ ] Cả 2 UI build & chạy trong `docker compose`; CORS + OIDC redirect URIs cấu hình đúng.
- [ ] Pipeline CI bắt buộc chạy và pass: `dotnet test` (backend), unit tests frontend, và e2e smoke tests.
- [ ] CI/CD pipeline xanh (bao gồm cả image Angular + Blazor).
- [ ] README cập nhật hướng dẫn chạy local + production (kèm 2 UI).

---

## 6. Budget & Cadence

- **Mỗi Wave** ≈ 1 section của course.
- Checkpoint commit cuối mỗi wave + tag (`v0.2-product`, `v0.3-customer`, …).
- Nếu >75% budget của 1 wave đã dùng mà <50% task done → **dừng, reassess** (giảm scope hoặc tách wave).
- Không skip review để chạy nhanh — thay vào đó giảm scope.

---

## 7. References

- Architecture: `microservice_course_architecture.png`
- Course outline: `content_course.txt`
- Skill: `.windsurf/skills/executing-plans/SKILL.md`
- Current code anchors:
  - `@/Users/Personal/aspnetcore.microservices/Product.API/Controllers/ProductsController.cs:1-81`
  - `@/Users/Personal/aspnetcore.microservices/Product.API/Repositories/ProductRepository.cs:1-34`
  - `@/Users/Personal/aspnetcore.microservices/docker-compose.override.yml:1-107`

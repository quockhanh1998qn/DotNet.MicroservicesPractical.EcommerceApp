# Code Review Findings – Waves 0, 1, 2

> Tổng hợp findings sau khi review từng wave. Status tổng: **NEEDS_CHANGES** (không có Critical, một số Important cần xử lý trước khi đóng Wave 2 và trước khi bắt đầu Wave 3).

Severity legend:

| Mã | Ý nghĩa | Hành động |
|---|---|---|
| 🔴 Critical | Block merge | Phải fix |
| 🟡 Important | Cần fix | Fix sớm hoặc tạo issue tracked |
| 💭 Minor | Cải thiện | Tuỳ chọn |

---

## Wave 0 – Shared Foundation Hardening

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T0.1 Audit `Contracts` | ✅ | `EntityBase`, `EntityAuditBase`, `IRepositoryBaseAsync`, `IUnitOfWork`, `IDateTracking`, `IAuditTable`, `IUserTracking` đã có |
| T0.2 Audit `Infrastructure.Common` | ✅ | `RepositoryBaseAsync`, `UnitOfWork` đầy đủ CRUD + transaction |
| T0.3 Audit `Common.Logging.Serilogger` | ✅ | File + Console sink. Elasticsearch sink để Wave 9 |
| T0.4 `Shared/Configurations` | ⚠️ | Đã có `DatabaseSettings`, `EmailSettings` nhưng chưa được bind/sử dụng ở bất kỳ service nào |

### Stage 2 — Code Quality

#### 🟡 Important

- **`Shared.Configurations` chưa được dùng** — `@c:\Users\Personal\aspnetcore.microservices\Shared\Configurations\DatabaseSettings.cs:1-11` và `@c:\Users\Personal\aspnetcore.microservices\Shared\Configurations\EmailSettings.cs:1-17` được khai báo nhưng cả Product.API và Customer.API đều đọc `ConnectionStrings:DefaultConnectionString` trực tiếp, không bind qua `IOptions<DatabaseSettings>`. Dead code → tiêu chuẩn cấu hình toàn dự án không thực sự tồn tại.
- **`RepositoryBaseAsync.EndTransactionAsync` nuốt rollback** — `@c:\Users\Personal\aspnetcore.microservices\Infrastructure\Common\RepositoryBaseAsync.cs:54-58`. Nếu `SaveChangesAsync` throw, `CommitTransactionAsync` không chạy nhưng transaction chưa rollback. Nên wrap try/catch và rollback rồi rethrow, hoặc đặt tên đúng nghiệp vụ (`CommitTransactionAsync`).
- **`UnitOfWork<TContext>.Dispose` dispose `DbContext`** — `@c:\Users\Personal\aspnetcore.microservices\Infrastructure\Common\UnitOfWork.cs:15`. Trong ASP.NET Core, `DbContext` thuộc DI scope; DI sẽ dispose. UnitOfWork dispose lại → potential double-dispose nếu cùng scope.
- **`RepositoryBaseAsync.SaveChangesAsync()` không hỗ trợ `CancellationToken`** — `@c:\Users\Personal\aspnetcore.microservices\Infrastructure\Common\RepositoryBaseAsync.cs:99` trong khi `IUnitOfWork.SaveChangesAsync(CancellationToken)` có. Mất khả năng huỷ request đúng cách.
- **`Common.Logging` dùng Serilog 5.x trên `net10.0`** — `@c:\Users\Personal\aspnetcore.microservices\Common.Logging\Common.Logging.csproj:10-13`. `Serilog.AspNetCore 5.0.0` quá cũ, không tương thích chính thức .NET 10. Nâng lên ≥ 8.x trước Wave 9.

#### 💭 Minor

- **Nullable warning tiềm ẩn** — `@c:\Users\Personal\aspnetcore.microservices\Contracts\Domains\EntityBase.cs:7` `public TKey Id { get; set; }` không có `= default!`; với `<Nullable>enable</Nullable>` sẽ phát warning nếu `<TreatWarningsAsErrors>` bật. Tương tự `IUserTracking.CreatedBy/LastModifiedBy` ở `@c:\Users\Personal\aspnetcore.microservices\Contracts\Domains\Interfaces\IUserTracking.cs:5-7`.
- **`GetByIdAsync` lệ thuộc `x.Id.Equals(id)`** — `@c:\Users\Personal\aspnetcore.microservices\Infrastructure\Common\RepositoryBaseAsync.cs:44-50`. Nếu `id` null sẽ throw NRE. Thêm guard `ArgumentNullException.ThrowIfNull(id)`.
- **`Serilogger.ApplicationName.ToLower()`** — `@c:\Users\Personal\aspnetcore.microservices\Common.Logging\Serilogger.cs:11` dùng culture-sensitive. Đổi sang `ToLowerInvariant()`.
- **`Infrastructure.Common.UnitTests` test coverage cho transaction APIs còn thiếu** — `BeginTransactionAsync/EndTransactionAsync/RollbackTransactionAsync` chưa có test (mặc dù khó test với InMemory provider — cần SQLite hoặc Testcontainers).

---

## Wave 1 – Product.API + Containerize

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T1.1 DTOs Product | ✅ | `ProductDto`, `CreateProductDto`, `UpdateProductDto` |
| T1.2 MappingProfile | ✅ | 3 chiều mapping cơ bản |
| T1.3 Refactor `ProductsController` | ✅ | Nhận DTO, trả `ProductDto`, có `[ProducesResponseType]` |
| T1.4 `IProductRepository` + DI | ✅ | `ServiceExtensions.AddInfrastructureServices` |
| T1.5 `ProductContext` + Migrations + Seed | ✅ | Init migration + seed 11 sản phẩm |
| T1.6 `appsettings.json` MySQL | ⚠️ | Cleartext credentials trong file commit |
| T1.7 Dockerfile + compose | ✅ | Runtime-only image + service `product.api` |
| T1.8 Smoke test | ✅ | Đã xác nhận trong Progress Log |

### Stage 2 — Code Quality

#### 🟡 Important

- **Cleartext DB credentials trong `appsettings.json`** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\appsettings.json:2-8`. Dù chỉ là local dev, một khi vào nhánh chính sẽ là tiền lệ xấu. Dùng user-secrets hoặc đẩy vào `appsettings.Development.json` (gitignore) + env trong compose.
- **Không có unique index trên `No`** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Entities\CatalogProduct.cs:9-11` đánh `[Required]` nhưng không có `Index(IsUnique=true)`. Endpoint `CreateProduct` không kiểm tra trùng `No`, hai request đồng thời có thể tạo trùng. Cần migration thêm unique index + check trong controller hoặc bắt `DbUpdateException`.
- **`GetProductByNo` case-sensitive trên MySQL** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Repositories\ProductRepository.cs:21-22`. Phụ thuộc collation cột; nên normalize hoặc dùng `EF.Functions.Like` để rõ ý định.
- **`HostExtensions.MigrateDatabase` nuốt exception** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Extensions\HostExtensions.cs:22-27`. Log error rồi tiếp tục chạy app với DB chưa migrate. Production sẽ rất khó debug; nên rethrow để app fail-fast.
- **`ProductContext.SaveChangesAsync` không xử lý `Deleted` đúng cách** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Persistence\ProductContext.cs:17-44`. `Where` lọc `Modified/Added/Deleted` nhưng `switch` chỉ có 2 case → branch `Deleted` không làm gì. Đồng thời `item.State = EntityState.Added/Modified` trong case là dead code (đã là state đó rồi).
- **AutoMapper bản 12 + cơ chế đăng ký lạc hậu** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Extensions\ServiceExtensions.cs:41`. `AddAutoMapper(cfg => cfg.AddProfile(new MappingProfile()))` không scan assembly; mọi profile mới sẽ phải đăng ký thủ công. Dùng `services.AddAutoMapper(typeof(MappingProfile).Assembly)` để future-proof.
- **`ApplicationExtensions.UseInfrastructure` dùng pattern cũ** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Extensions\ApplicationExtensions.cs:11-19`. `UseRouting()` + `UseEndpoints(... MapDefaultControllerRoute())` là pattern .NET 5. Chuyển sang `app.MapControllers()` (minimal hosting) sạch hơn và đồng nhất với Customer.API.
- **`Pomelo.EntityFrameworkCore.MySql 6.0.1` trên `net10.0`** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Product.API.csproj:12`. EF Core 6 không hỗ trợ chính thức .NET 10; nâng lên Pomelo 8.x/9.x đồng thời cập nhật `MySqlServerVersion` config-driven.
- **`ModelState.IsValid` redundant trong `[ApiController]`** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Controllers\ProductsController.cs:59-62` và `:78-81`. `[ApiController]` tự động trả `400` với `ValidationProblemDetails`. Đoạn check trên không bao giờ chạy → có thể xoá để code gọn.

#### 💭 Minor

- **Hard-coded MySQL version** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Extensions\ServiceExtensions.cs:35`. `new Version(8, 0, 29)` cố định; nên đưa vào config (`DatabaseSettings.ServerVersion`).
- **`WeatherForecastController` + `WeatherForecast.cs` còn sót** — template mặc định, không được dùng.
- **`Dockerfile` không HEALTHCHECK / chạy root** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Dockerfile:1-11`. Plan có Wave 9 cho healthchecks; sau Wave 9 nên thêm `HEALTHCHECK` và `USER` non-root.
- **`Program.cs` `SeedProductAsync(...).Wait()`** — blocking startup. Pattern xuyên suốt project; chấp nhận nhưng nên track migrate sang `MigrateDatabaseAsync`.
- **Seed có entry "Lotus/Esprit"** với mô tả y học không liên quan (`@c:\Users\Personal\aspnetcore.microservices\Product.API\Persistence\ProductContextSeed.cs:23-30`) — có vẻ dữ liệu test cũ. Cleanup khi tiện.
- **`IProductRepository` kế thừa `IRepositoryBaseAsync<...>`** — `@c:\Users\Personal\aspnetcore.microservices\Product.API\Repositories\Interfaces\IProductRepository.cs:7`. Expose toàn bộ base API ra consumer, làm yếu domain boundary. Cùng pattern với Customer.API — cần thống nhất nguyên tắc.
- **Test coverage cho `ProductsController`** — `@c:\Users\Personal\aspnetcore.microservices\tests\Product.API.UnitTests\UnitTest1.cs:1-246` chỉ test Create/Update-NotFound/Delete-NotFound/Delete-OK/Mapping. Thiếu: `GetProducts`, `GetProductById` (cả hai branch), `GetProductByNo`, `UpdateProduct` success, validation error path. Đồng thời `InMemoryProductRepository` viết tay 130+ dòng (`@c:\Users\Personal\aspnetcore.microservices\tests\Product.API.UnitTests\UnitTest1.cs:115-244`) → dễ vỡ. Thay bằng Moq/NSubstitute sẽ ngắn hơn nhiều.

---

## Wave 2 – Customer.API (Minimal API + PostgreSQL)

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T2.1 Project Minimal API | ✅ | `Customer.API.csproj` |
| T2.2 Entity + DTOs | ✅ | `CustomerEntity` + DTOs ở `Shared/DTOs/Customer` |
| T2.3 CustomerContext + EF Core + Npgsql + Seed | ✅ | `CustomerContext`, migrations, `CustomerContextSeed` |
| T2.4 Repository Pattern | ✅ | `ICustomerRepository`, `CustomerRepository` |
| T2.5 Minimal API CRUD `/api/customers` | ✅ | `Program.cs` map group |
| T2.6 ServiceExtensions + AutoMapper | ✅ | `ServiceExtensions.AddInfrastructure` |
| T2.7 Dockerfile + compose | ✅ | `customer.api` service |
| T2.8 Smoke test | ✅ | Confirmed in Progress Log |

### Stage 2 — Code Quality

#### 🟡 Important

- **Bỏ qua DataAnnotations validation trong Minimal API** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Program.cs:38`. `CreateCustomerDto`/`UpdateCustomerDto` có `[Required]/[EmailAddress]/[StringLength]`, nhưng Minimal API **không tự** chạy DataAnnotations. Endpoint hiện accept payload rỗng/email sai. Đề nghị `MinimalApis.Extensions.WithParameterValidation()` hoặc thêm FluentValidation, hoặc gọi `Validator.TryValidateObject` rồi trả `Results.ValidationProblem`.
- **Race condition trên uniqueness email** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Program.cs:40-48`. Kiểm tra `GetCustomerByEmail` rồi `CreateCustomer` không atomic. Cần unique index trên cột Email (migration) + bắt `DbUpdateException` để trả `409`.
- **So sánh email case-sensitive trên PostgreSQL** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Repositories\CustomerRepository.cs:21-22`. `x.Email.Equals(email)` → `=` case-sensitive. Email vốn coi như case-insensitive: normalize lower-case khi lưu hoặc dùng `EF.Functions.ILike`/cột `citext`.
- **Cleartext DB credentials trong `appsettings.json`** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\appsettings.json:2-8`.
- **Exception handling khác pattern Product.API** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Program.cs:94-102`. Thiếu xử lý `StopTheHostException` như `@c:\Users\Personal\aspnetcore.microservices\Product.API\Program.cs:26-35`. Design tools (`dotnet ef`) sẽ bị log nuốt fatal nhầm.
- **`UseAuthorization` không có Authentication scheme** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Program.cs:23`. Cấu hình authorization chưa có scheme nào (Identity Server thuộc Wave 10) → no-op + warning. Bỏ tạm thời cho tới Wave 10 hoặc thêm TODO rõ ràng.
- **Trùng query khi Delete** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Program.cs:75-85` + `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Repositories\CustomerRepository.cs:28-35`. Endpoint đã `GetCustomer(id)` rồi repository lại `GetCustomer(id)` lần nữa → 2 roundtrip DB. Đổi sang `DeleteAsync(entity)` trực tiếp.
- **Pomelo / Npgsql / EF Core 6 trên `net10.0`** — cùng nhóm version-mismatch với Wave 1.

#### 💭 Minor

- **WeatherForecast template** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Controllers\WeatherForecastController.cs:1` vẫn còn dù dùng Minimal API.
- **`Customer.API.http`** chỉ có 135 bytes — thiếu request CRUD mẫu để dev test nhanh.
- **Seed dùng `.Wait()`** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Program.cs:88-91` blocking startup. Đồng nhất với Product.API nhưng nên track.
- **Repository interface kế thừa `IRepositoryBaseAsync<...>`** — `@c:\Users\Personal\aspnetcore.microservices\Customer.API\Repositories\Interfaces\ICustomerRepository.cs:7` expose mọi method base.
- **Test coverage còn mỏng theo DoD mới** — `@c:\Users\Personal\aspnetcore.microservices\tests\Customer.API.UnitTests\UnitTest1.cs:1-103` có 4 test (mapping, create, get-missing, delete). Thiếu test cho: `UpdateCustomer` (cả case trùng email), `GetCustomers` enumeration, validation rules.

---

## Cross-cutting Themes (tổng kết qua 3 wave)

Một số vấn đề lặp lại — nên xử lý đồng bộ trước Wave 3:

1. **Version mismatch EF Core 6.0.x vs `net10.0`** ở Product.API, Customer.API, Common.Logging (Serilog 5.0.0). Nâng đồng bộ.
2. **Cleartext credentials trong `appsettings.json`** ở cả hai service.
3. **`IXxxRepository : IRepositoryBaseAsync`** rò rỉ base API qua consumer.
4. **Domain boundary uniqueness** (Product.No, Customer.Email) thiếu unique index + check trùng atomic.
5. **`SeedXxxAsync(...).Wait()`** blocking startup.
6. **`MigrateDatabase` nuốt exception** không fail-fast.
7. **`WeatherForecast` template dead code** trong cả Product.API & Customer.API.
8. **Test coverage chưa đạt DoD mới** (đặc biệt cho controllers/endpoints và validation paths).
9. **`Shared.Configurations` chưa được dùng** dù đã định nghĩa.

---

## Status Summary

| Wave | Status | Critical | Important | Minor |
|---|---|---:|---:|---:|
| 0 | NEEDS_CHANGES | 0 | 5 | 4 |
| 1 | NEEDS_CHANGES | 0 | 8 | 6 |
| 2 | NEEDS_CHANGES | 0 | 8 | 5 |

Tổng quát: spec compliance đạt cả 3 wave, chỉ vướng code quality. Không có Critical → có thể tiếp tục Wave 3 song song với việc xử lý Important findings dưới dạng tasks dọn dẹp.

---

## Recommended Next Actions (Waves 0–2)

1. Mở mini-PR "wave-0-1-2 hardening" cover:
   - Unique index Product.No + Customer.Email + duplicate check.
   - Lowercase normalization email; chuẩn hoá search Product.No.
   - Loại bỏ cleartext credentials, dùng `appsettings.Development.json` gitignored + env trong compose.
   - Đồng bộ `Program.cs` exception handling (StopTheHostException, không nuốt migration error).
   - Bỏ `UseAuthorization` ở Customer.API tới Wave 10.
   - Bind `IOptions<DatabaseSettings>` ở `ServiceExtensions` thay vì đọc connection string trực tiếp.
   - Xoá `WeatherForecastController` + `WeatherForecast.cs` ở cả 2 service.
   - Refactor `RepositoryBaseAsync.EndTransactionAsync` để rollback khi SaveChanges throw.
2. Bổ sung unit tests còn thiếu:
   - Customer: Update (happy + duplicate email), GetCustomers, validation rules.
   - Product: GetProducts, GetProductById (both branches), GetProductByNo, UpdateProduct success, validation error path; cân nhắc thay InMemoryProductRepository bằng Moq.
3. Lập kế hoạch nâng version: EF Core, Pomelo, Npgsql, Serilog.AspNetCore → đồng nhất với `net10.0` (có thể tạo wave "tech-debt-upgrade" nếu phức tạp).
4. Cập nhật Progress Log trong `README.md` sau khi hoàn tất mini-PR.

---

---

# Code Review Findings – Waves 3–12

> Review session: 2026-06-29. Tổng hợp findings sau khi Waves 3–10 hoàn thành, Wave 11 (Angular) partial, Wave 12 (Admin.Blazor) hoàn thành.

---

## Wave 3 – Basket.API + Redis

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T3.1 Cart + CartItem entities + DTOs | ✅ | `Cart`, `CartItem`, `CartDto`, `CartItemDto`, `BasketCheckoutDto` đầy đủ |
| T3.2 IBasketRepository + BasketRepository | ✅ | IDistributedCache, JSON payload, key `basket:<user>`, 7d/2h sliding TTL |
| T3.3 StackExchangeRedis + DI | ✅ | `ConfigureRedis` trong `ServiceExtensions` |
| T3.4 Endpoints GET/POST/DELETE + Checkout | ✅ | `BasketsController` với stock validation via gRPC |
| T3.5 Dockerfile + compose | ✅ | `basket.api` (5004) + `basketdb` + `GrpcSettings__StockUrl` env |

### Stage 2 — Code Quality

#### 🟡 Important

- **`ModelState.IsValid` redundant trong `[ApiController]`** — `@c:\Users\Personal\aspnetcore.microservices\Basket.API\Controllers\BasketsController.cs:52-55` và `:87-90`. Cùng pattern với Product.API (Wave 1 finding). `[ApiController]` tự trả `400`; các check này không bao giờ đạt `false`. Xoá để code gọn.
- **`AddAutoMapper` không scan assembly** — `@c:\Users\Personal\aspnetcore.microservices\Basket.API\Extensions\ServiceExtensions.cs:23`. `cfg.AddProfile(new MappingProfile())` — mỗi profile mới phải đăng ký thủ công. Đổi sang `AddAutoMapper(typeof(MappingProfile).Assembly)`.
- **Thiếu `StopTheHostException` handling** — `@c:\Users\Personal\aspnetcore.microservices\Basket.API\Program.cs:32-35`. Pattern catch-all nuốt cả `StopTheHostException` khi `dotnet ef` chạy design-time. Thêm guard như Product.API.

#### 💭 Minor

- **`InMemoryDistributedCache` nhân đôi giữa test files** — `@c:\Users\Personal\aspnetcore.microservices\tests\Basket.API.UnitTests\BasketRepositoryTests.cs:93-106` và `@c:\Users\Personal\aspnetcore.microservices\tests\Basket.API.UnitTests\CheckoutTests.cs:108-119` có cùng private class. Nên extract vào một `TestHelpers` chung.
- **Test coverage thiếu**: Checkout stock validation path (item quantity > available), `UpdateBasket` với username rỗng (sẽ throw ArgumentException).

---

## Wave 4 – Ordering.API (Clean Architecture + CQRS)

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T4.1 Layer split | ✅ | `Ordering.Domain`, `.Application`, `.Infrastructure`, `.API` |
| T4.2 Domain: Order aggregate, DomainEvents | ✅ | `AggregateRoot<long>`, `OrderCreated/Updated/Deleted`, `OrderStatus` |
| T4.3 Application: CQRS + FluentValidation | ✅ | Commands + Queries + Validators + MediatR handlers |
| T4.4 Infrastructure: OrderContext + SQL Server | ✅ | `OrderContext`, `OrderContextSeed`, `OrderRepository` |
| T4.5 Email service interface | ✅ | `ISmtpEmailService` + stub `SmtpEmailService` |
| T4.6 Thin API controller | ✅ | `OrdersController` delegates all to `IMediator` |
| T4.7 Dockerfile + compose | ✅ | `ordering.api` (5005) + `orderdb` |

### Stage 2 — Code Quality

#### 🟡 Important

- **`EnsureCreatedAsync()` thay vì `MigrateAsync()`** — `@c:\Users\Personal\aspnetcore.microservices\Ordering.API\Program.cs:80`. Không tạo migration history, xung đột khi cần migrate production. Khi SQL Server sẵn sàng, phải chuyển sang `context.Database.MigrateAsync()` + tạo init migration.
- **Seed exception bị nuốt** — `@c:\Users\Personal\aspnetcore.microservices\Ordering.API\Program.cs:83-86`. `catch` chỉ log error; app tiếp tục chạy với DB state không xác định. Cân nhắc rethrow hoặc fail-fast.
- **`AddAutoMapper` không scan assembly** — `@c:\Users\Personal\aspnetcore.microservices\Ordering.API\Program.cs:29`. `cfg.AddProfile(new OrderingEventBusMappingProfile())` — manual registration.

#### 💭 Minor

- **`command.Id = id` trong UpdateOrder** — `@c:\Users\Personal\aspnetcore.microservices\Ordering.API\Controllers\OrdersController.cs:53`. Nếu body có `Id` khác route `id`, body bị overwrite im lặng. Thêm guard hoặc validate sớm.
- **FluentValidation deprecation** — `AddFluentValidationAutoValidation` + `AddFluentValidationClientsideAdapters` ở `@c:\Users\Personal\aspnetcore.microservices\Ordering.API\Program.cs:21-22` — clientside adapters là Razor Pages pattern, không áp dụng cho API. Xoá `AddFluentValidationClientsideAdapters`.

---

## Wave 5 – Microservices Communication (RabbitMQ + MassTransit)

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T5.2 EventBus.Messages | ✅ | `IntegrationBaseEvent`, `BasketCheckoutEvent`, `EventBusConstants` |
| T5.3 Basket.API publisher | ✅ | `POST /api/baskets/checkout` → `IPublishEndpoint` → RabbitMQ |
| T5.4 Ordering.API consumer | ✅ | `BasketCheckoutEventConsumer` → `CreateOrderCommand` via MediatR |
| T5.5 compose dependencies | ✅ | `rabbitmq` wired |
| T5.1 RabbitMQ.Demo | ⚠️ | Không thấy `Samples/RabbitMQ.Demo` trong cây thư mục — bỏ qua |
| T5.6/T5.7 Event Sourcing | ⚠️ | Domain events có trong Ordering.Domain nhưng chưa có dispatch mechanism |

### Stage 2 — Code Quality

#### 🟡 Important

- **Redundant double-read config** — `@c:\Users\Personal\aspnetcore.microservices\Ordering.API\Program.cs:32` khai báo `rabbitmqHost`, rồi `:39-40` khai báo `eventBusHost` đọc cùng key `EventBusSettings:HostAddress`. Xoá một trong hai.
- **Fallback cleartext RabbitMQ credentials** — `@c:\Users\Personal\aspnetcore.microservices\Ordering.API\Program.cs:32`. `"amqp://guest:guest@localhost:5672"` là hardcoded fallback. Nên `throw InvalidOperationException` nếu config thiếu (tương tự `Basket.API.Extensions.ServiceExtensions.ConfigureMassTransit`).

#### 💭 Minor

- **T5.1 và T5.6/T5.7 chưa hoàn thành** — domain events phát sinh từ `Order.MarkCreated/Updated/Deleted` nhưng không có dispatcher tích hợp vào `DbContext.SaveChangesAsync`. Nếu spec yêu cầu dispatch, cần `IDomainEventDispatcher` hook.

---

## Wave 6 – Inventory.API + MongoDB + gRPC

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T6.1-T6.3 Entities + Repo + Service | ✅ | MongoDB, paged query, stock sum, Purchase/Sale service |
| T6.4 REST CRUD | ✅ | `InventoriesController` đầy đủ routes |
| T6.5-T6.6 gRPC server | ✅ | `stock.proto`, `StockProtoServiceImpl`, `MapGrpcService` |
| T6.7 Basket.API gRPC client | ✅ | `StockService`, `AddGrpcClient`, Polly retry |
| T6.8 Dockerfile + compose | ✅ | `inventory.api` (5006) + `inventorydb` |

### Stage 2 — Code Quality

#### 🟡 Important

- **`ModelState.IsValid` redundant** — `@c:\Users\Personal\aspnetcore.microservices\Inventory.API\Controllers\InventoriesController.cs:52` và `:62`. Cùng pattern với Wave 1/3. `[ApiController]` xử lý validation tự động.
- **`AddAutoMapper` không scan assembly** — `@c:\Users\Personal\aspnetcore.microservices\Inventory.API\Extensions\ServiceExtensions.cs:23`.
- **Thiếu `StopTheHostException` handling** — `@c:\Users\Personal\aspnetcore.microservices\Inventory.API\Program.cs:32-35`.

#### 💭 Minor

- **Test coverage thiếu**: `GetAllByItemNo` (pagination meta), `GetInventoryById` (found branch), `Delete` (happy path), controller-level tests.
- **gRPC `Http2` Kestrel config** — không thấy explicit Kestrel `Http2` endpoint config trong `appsettings.json` hoặc `ServiceExtensions`. Trong Docker, gRPC hoạt động nếu Inventory lắng nghe plain HTTP/2 (không TLS). Cần xác nhận `ASPNETCORE_URLS` hoặc Kestrel config hỗ trợ đúng port cho gRPC.

---

## Wave 7 – Ocelot API Gateway

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T7.1 ocelot route files | ✅ | 6 file JSON, auto-merged |
| T7.2 JWT Bearer placeholder | ✅ | `"Bearer"` scheme với Identity Server authority |
| T7.3 Rate limit + QoS + Cache | ✅ | `AddCacheManager`, `AddPolly`, per-route `RateLimitOptions` |
| T7.4 Dockerfile + compose | ✅ | `ocelot.gateway` (5000) |

### Stage 2 — Code Quality

#### 🟡 Important

- **`RequireHttpsMetadata = false` hardcoded** — `@c:\Users\Personal\aspnetcore.microservices\OcelotApiGw\Program.cs:25`. Nên đọc từ config (`IdentityServer:RequireHttpsMetadata`) để dễ bật lại cho production.
- **CORS origins hardcoded** — `@c:\Users\Personal\aspnetcore.microservices\OcelotApiGw\Program.cs:37-38`. `"http://localhost:4200", "http://localhost:5100"` hard-coded thay vì config-driven. Production sẽ cần domain thực.

#### 💭 Minor

- **Không có unit tests** — Expected cho API Gateway; integration/e2e test khi docker-compose chạy là phù hợp.

---

## Wave 8 – HangFire.API + Background Jobs

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T8.1 Hangfire SQL Server | ✅ | `UseSqlServerStorage`, `AddHangfireServer` |
| T8.2 Dashboard `/hangfire` | ✅ | `UseHangfireDashboard` với `HangfireDashboardAuthorizationFilter` |
| T8.3 Basket reminder job | ✅ | `IBasketReminderJob`, `SmtpEmailService`, Polly on HttpClient |
| T8.4 Dockerfile + compose | ✅ | `hangfire.api` (5007) |

### Stage 2 — Code Quality

#### 🔴 Critical

- **`EnsureDatabaseExists` blocking synchronous SQL** — `@c:\Users\Personal\aspnetcore.microservices\HangFire.API\Extensions\ServiceExtensions.cs:103-120`. Gọi `connection.Open()` và `command.ExecuteNonQuery()` **đồng bộ trong DI registration** (`AddHangfireServices`). Nếu SQL Server chưa sẵn sàng, exception không được catch ở đây, ném ra tại `services.AddInfrastructure(...)` → crash startup với stack trace khó debug. Cần bọc trong `try/catch` có log, hoặc chuyển sang async startup action (`IHostedService`).

#### 🟡 Important

- **`IgnoreAntiforgeryToken = true` trên Hangfire dashboard** — `@c:\Users\Personal\aspnetcore.microservices\HangFire.API\Program.cs:35`. Tắt CSRF protection cho dashboard. Chấp nhận nếu Hangfire dashboard chỉ dùng nội bộ (không public-facing), nhưng nên có TODO track cho production.
- **`WeatherForecast.cs` template vẫn còn** — `@c:\Users\Personal\aspnetcore.microservices\HangFire.API\WeatherForecast.cs`. Dead code từ template.
- **Thiếu unit tests** — HangFire.API không có test project. `BasketReminderJob` logic nên có ít nhất test cho flow "customer có basket chưa checkout → gửi email".

#### 💭 Minor

- **Swagger chỉ bật `IsDevelopment`** — `@c:\Users\Personal\aspnetcore.microservices\HangFire.API\Program.cs:21-25`. Không nhất quán với Basket/Inventory/Ordering luôn bật Swagger.

---

## Wave 9 – Observability + Resilience + HealthChecks

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T9.1 Elasticsearch sink | ✅ | Kích hoạt khi `ElasticConfiguration:Uri` có giá trị |
| T9.2 Kibana dashboard | ✅ | Documented in README |
| T9.3 Correlation ID middleware | ✅ | `CorrelationIdMiddleware` + `UseCorrelationId()` extension |
| T9.4-T9.5 Polly | ✅ | Basket→gRPC, HangFire→Basket HTTP |
| T9.6 HealthChecks | ✅ | Mỗi service expose `/health` + UI JSON writer |
| T9.7 WebHealthStatus | ✅ | `webhealth.status` service aggregates 6 services |
| T9.8 Outbox | ✅ | MassTransit `UseInMemoryOutbox` + retry trên Ordering consumer |

### Stage 2 — Code Quality

#### 🟡 Important

- **`WebHealthStatus` thiếu HealthChecks cho Identity Server + WebApp.Angular** — `@c:\Users\Personal\aspnetcore.microservices\docker-compose.override.yml:242-262`. Dashboard chỉ liệt kê 6 services (Product→HangFire), thiếu `identity.server` và `admin.blazor`.

#### 💭 Minor

- **Không có unit tests cho `Common.Logging.Serilogger`** — Chỉ có `CorrelationIdMiddleware` tests. `Serilogger.Configure` config không được test.

---

## Wave 10 – Identity Server + System-wide Auth

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T10.1-T10.2 Identity.Server project + config | ✅ | Duende 7, EF Config/Operational stores, scopes, clients |
| T10.3 Migrations | ✅ | 9 migration files trong `Identity.Server/Migrations/` |
| T10.4 ASP.NET Identity | ✅ | `User`, `Role`, `IdentityContext`, `UserRepository` |
| T10.5 Email flows | ✅ | `EmailService`, `IEmailService` |
| T10.6-T10.7 Permission model | ✅ | `PermissionRepository` + Dapper + stored procs |
| T10.8 Bearer policy | ✅ | `Common.Auth.AddMicroserviceAuthentication` áp dụng toàn bộ services |
| T10.9-T10.10 Auth trên services | ✅ | `AuthSettings` env vars trong compose; tất cả services dùng `Common.Auth` |
| T10.11 JWT auth Ocelot | ✅ | `"Bearer"` scheme + Identity Server authority |
| T10.12 Containerize | ✅ | `identity.server` (5009) + depends `orderdb` |

### Stage 2 — Code Quality

#### 🔴 Critical

- **Wildcard CORS trong Identity Server** — `@c:\Users\Personal\aspnetcore.microservices\Identity.Server\Program.cs:82`. `SetIsOriginAllowed(_ => true)` cho phép **mọi origin** gọi Identity Server — CSRF risk khi deploy production. Thay bằng explicit whitelist (`WithOrigins("http://localhost:4200", "http://localhost:5100")`) giống OcelotApiGw.

#### 🟡 Important

- **Seed failure swallowed** — `@c:\Users\Personal\aspnetcore.microservices\Identity.Server\Program.cs:122-124`. Seed error chỉ log, không rethrow → user/permission không được tạo nhưng app vẫn chạy.
- **`AddDeveloperSigningCredential()`** — `@c:\Users\Personal\aspnetcore.microservices\Identity.Server\Program.cs:61`. Regenerates key trên restart → mọi access token vô hiệu. Phù hợp dev nhưng cần TODO cho production certificate.
- **`MapDefaultControllerRoute()` dư** — `@c:\Users\Personal\aspnetcore.microservices\Identity.Server\Program.cs:109`. `MapControllers()` (line 108) đã đủ cho API controllers; `MapDefaultControllerRoute()` thêm default route `{controller=Home}/{action=Index}` — redundant nếu Razor views có route riêng.
- **Không có unit test cho Identity.Server** — `PermissionRepository`, `UserRepository`, `IdentityContextSeed` không có test project.

#### 💭 Minor

- **`tempkey.jwk` commit vào source** — `@c:\Users\Personal\aspnetcore.microservices\Identity.Server\tempkey.jwk`. Developer signing key không nên commit; thêm vào `.gitignore`.

---

## Wave 11 – Angular WebApp

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T11.1 Project structure | ✅ | Angular 17 standalone, `src/app/` exists |
| T11.2 Tailwind CSS | ✅ | `tailwind.config.js`, `postcss.config.js` |
| T11.3 API client services | ⚠️ | Cần verify trong `src/app/` |
| T11.4 angular-oauth2-oidc | ✅ | `package.json` có `angular-oauth2-oidc@17` |
| T11.5-T11.9 Features (product/basket/order) | ⚠️ | `src/app/` có 26 items nhưng chưa verify đầy đủ |
| T11.10 Unit tests (Jest) | ✅ | Jest configured via `@angular-builders/jest` |
| T11.11 Dockerfile + nginx | ✅ | Multi-stage `Dockerfile` + `nginx.conf` |
| T11.12 CORS + redirect URIs | ✅ | OcelotApiGw CORS updated, compose wired |

### Stage 2 — Code Quality

#### 🟡 Important

- **`node_modules/` trống** — `@c:\Users\Personal\aspnetcore.microservices\WebApp.Angular` chưa có `node_modules` (0 items). `npm install` chưa chạy trong môi trường hiện tại; Docker build vẫn hoạt động nếu Dockerfile tự chạy `npm ci`, nhưng local `ng serve` sẽ fail.
- **T11.3-T11.9 chưa verify** — Không thể xác nhận đủ feature implementation từ file listing 26 items. Cần `ng build` sạch và smoke test trên browser.
- **Thiếu e2e tests** — T11.10 spec yêu cầu Playwright/Cypress cho login→cart→checkout flow. Chưa thấy e2e test config trong project.

---

## Wave 12 – Admin.Blazor

### Stage 1 — Spec Compliance

| Task | Trạng thái | Ghi chú |
|---|---|---|
| T12.1 Blazor Server project | ✅ | `Admin.Blazor`, MudBlazor, InteractiveServer |
| T12.2 Layout + navigation | ✅ | Components/ có 15 items |
| T12.3 OIDC auth + role Admin | ✅ | Cookie + OpenIdConnect, role mapping, fallback policy |
| T12.4 Typed HttpClient per service | ✅ | `ProductApiClient`, `CustomerApiClient`, `OrderApiClient`, `InventoryApiClient` |
| T12.5-T12.11 Admin pages | ⚠️ | Components/ có 15 items; cần verify từng page |
| T12.12 bUnit tests | ❌ | Không thấy bUnit test project |
| T12.13 Dockerfile + compose | ✅ | `admin.blazor` (5100) |

### Stage 2 — Code Quality

#### 🔴 Critical

- **Hardcoded OIDC client secret trong source** — `@c:\Users\Personal\aspnetcore.microservices\Admin.Blazor\Program.cs:44`. `options.ClientSecret = "blazor-admin-secret"` commit vào repo. Dù là dev, cần chuyển sang `appsettings.Development.json` (gitignored) hoặc user-secrets + env var trong compose.

#### 🟡 Important

- **Authority fallback localhost không hoạt động trong Docker** — `@c:\Users\Personal\aspnetcore.microservices\Admin.Blazor\Program.cs:42`. `?? "http://localhost:5009"` sẽ fail khi chạy trong container (localhost trong container ≠ host). Nên `throw` nếu `IdentityServer:Authority` thiếu, giống các service khác.
- **Credentials hint trong Access Denied HTML** — `@c:\Users\Personal\aspnetcore.microservices\Admin.Blazor\Program.cs:170-173`. HTML inline có `admin@tedu.local` / `Admin@123!` — lộ thông tin tài khoản admin trong response 403. Xoá hint credentials khỏi production HTML.
- **Duplicated role claim mapping logic** — `@c:\Users\Personal\aspnetcore.microservices\Admin.Blazor\Program.cs:71-102`. `OnTokenValidated` (lines 71-81) và `OnUserInformationReceived` (82-102) cùng parse và add role claims với logic gần trùng. Extract sang `IClaimsTransformation` hoặc shared helper.
- **Thiếu bUnit tests** — T12.12 yêu cầu component tests nhưng không có test project.

#### 💭 Minor

- **`MassTransit` dùng `StockUpdateConsumer` nhưng không thấy `StockUpdateMessage` contract** — Consumer `StockUpdateConsumer` đăng ký tại line 128; cần đảm bảo `EventBus.Messages` export đúng message type để AdminBlazor nhận được.

---

## Cross-cutting Themes (Waves 3–12)

| # | Issue | Severity | Affected |
|---|---|---|---|
| 1 | `ModelState.IsValid` redundant trong `[ApiController]` | 🟡 | Basket.API, Inventory.API |
| 2 | `AddAutoMapper(cfg.AddProfile(new X()))` không scan assembly | 🟡 | Basket.API, Ordering.API, Inventory.API |
| 3 | Thiếu `StopTheHostException` guard | 🟡 | Basket.API, Inventory.API |
| 4 | Cleartext fallback credentials (RabbitMQ) | 🟡 | Ordering.API |
| 5 | Hardcoded OIDC client secret | 🔴 | Admin.Blazor |
| 6 | Wildcard CORS trong Identity Server | 🔴 | Identity.Server |
| 7 | `EnsureDatabaseExists` blocking sync startup | 🔴 | HangFire.API |
| 8 | `WeatherForecast.cs` template dead code | 💭 | HangFire.API |
| 9 | `tempkey.jwk` commit vào source | 💭 | Identity.Server |
| 10 | Thiếu bUnit tests | 🟡 | Admin.Blazor (T12.12) |
| 11 | Thiếu e2e tests | 🟡 | Angular (T11.10) |

---

## Status Summary (Waves 3–12)

| Wave | Status | Critical | Important | Minor |
|---|---|---:|---:|---:|
| 3 – Basket.API | APPROVED_WITH_NOTES | 0 | 3 | 2 |
| 4 – Ordering.API | APPROVED_WITH_NOTES | 0 | 3 | 2 |
| 5 – RabbitMQ | APPROVED_WITH_NOTES | 0 | 2 | 1 |
| 6 – Inventory.API | APPROVED_WITH_NOTES | 0 | 3 | 2 |
| 7 – Ocelot GW | APPROVED_WITH_NOTES | 0 | 2 | 1 |
| 8 – HangFire.API | NEEDS_CHANGES | 1 | 2 | 1 |
| 9 – Observability | APPROVED_WITH_NOTES | 0 | 1 | 1 |
| 10 – Identity Server | NEEDS_CHANGES | 1 | 4 | 1 |
| 11 – Angular | INCOMPLETE | 0 | 3 | 0 |
| 12 – Admin.Blazor | NEEDS_CHANGES | 1 | 4 | 1 |

**Tổng Critical mới: 3** (HangFire EnsureDatabaseExists blocking, Identity CORS wildcard, Admin.Blazor hardcoded secret)

---

## Recommended Next Actions (Waves 3–12)

**Ưu tiên cao (Critical — fix trước khi merge):**
1. **Admin.Blazor**: Xoá `ClientSecret` khỏi `Program.cs`, chuyển sang env var trong compose + user-secrets locally.
2. **Identity.Server**: Thay `SetIsOriginAllowed(_ => true)` bằng explicit whitelist cho `localhost:4200` và `localhost:5100`.
3. **HangFire.API**: Wrap `EnsureDatabaseExists` trong try/catch với log + graceful fail, hoặc chuyển sang `IHostedService` async.

**Ưu tiên vừa (Important — fix sớm):**
4. Xoá redundant `ModelState.IsValid` checks trong Basket.API và Inventory.API.
5. Đổi tất cả `AddAutoMapper(cfg.AddProfile(new X()))` sang `AddAutoMapper(typeof(X).Assembly)`.
6. Thêm `StopTheHostException` guard vào Basket.API và Inventory.API.
7. Xoá redundant `eventBusHost` đọc lại config trong Ordering.API Program.cs.
8. Xoá credential hint khỏi Access Denied HTML trong Admin.Blazor.
9. Chuyển Ordering.API sang `MigrateAsync()` + tạo EF migration khi SQL Server sẵn sàng.
10. Thêm `tempkey.jwk` vào `.gitignore`.

**Ưu tiên thấp (Minor / Tracking):**
11. Xoá `WeatherForecast.cs` khỏi HangFire.API.
12. Extract duplicated role claim mapping thành shared helper trong Admin.Blazor.
13. Thêm bUnit test project cho Admin.Blazor (T12.12).
14. Confirm `ng build` + e2e smoke test cho Angular (T11.10).
15. Xác nhận gRPC Http/2 config trong Inventory.API (Kestrel port).

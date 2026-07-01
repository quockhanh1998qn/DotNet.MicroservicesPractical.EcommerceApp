using Serilog;
using Common.Logging;
using Common.Logging.CorrelationId;
using AutoMapper;
using Customer.API.Entities;
using Customer.API.Extensions;
using Customer.API.Persistence;
using Customer.API.Repositories.Interfaces;
using Shared.DTOs.Customer;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Serilogger.Configure);
Log.Information("Start Customer API up");

try
{
	builder.Services.AddInfrastructure(builder.Configuration);

	var app = builder.Build();

	app.UseCorrelationId();

	app.UseSwagger();
	app.UseSwaggerUI();

	app.UseAuthentication();
	app.UseAuthorization();
	app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
	{
		Predicate = _ => true,
		ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse,
	});
	var customers = app.MapGroup("/api/customers");

	customers.MapGet("/", async (ICustomerRepository repository, IMapper mapper) =>
	{
		var entities = await repository.GetCustomers();
		return Results.Ok(mapper.Map<IEnumerable<CustomerDto>>(entities));
	});

	customers.MapGet("/{id:long}", async (long id, ICustomerRepository repository, IMapper mapper) =>
	{
		var entity = await repository.GetCustomer(id);
		return entity is null ? Results.NotFound() : Results.Ok(mapper.Map<CustomerDto>(entity));
	});

	customers.MapPost("/", async (CreateCustomerDto request, ICustomerRepository repository, IMapper mapper) =>
	{
		var existing = await repository.GetCustomerByEmail(request.Email);
		if (existing is not null)
		{
			return Results.Conflict(new { message = "Customer email already exists." });
		}

		var entity = mapper.Map<CustomerEntity>(request);
		await repository.CreateCustomer(entity);
		await repository.SaveChangesAsync();

		var response = mapper.Map<CustomerDto>(entity);
		return Results.Created($"/api/customers/{entity.Id}", response);
	});

	customers.MapPut("/{id:long}", async (long id, UpdateCustomerDto request, ICustomerRepository repository, IMapper mapper) =>
	{
		var entity = await repository.GetCustomer(id);
		if (entity is null)
		{
			return Results.NotFound();
		}

		var duplicatedEmail = await repository.GetCustomerByEmail(request.Email);
		if (duplicatedEmail is not null && duplicatedEmail.Id != id)
		{
			return Results.Conflict(new { message = "Customer email already exists." });
		}

		mapper.Map(request, entity);
		await repository.UpdateCustomer(entity);
		await repository.SaveChangesAsync();

		return Results.Ok(mapper.Map<CustomerDto>(entity));
	});

	customers.MapDelete("/{id:long}", async (long id, ICustomerRepository repository) =>
	{
		var entity = await repository.GetCustomer(id);
		if (entity is null)
		{
			return Results.NotFound();
		}

		await repository.DeleteCustomer(id);
		await repository.SaveChangesAsync();
		return Results.NoContent();
	});

	app.MigrateDatabase<CustomerContext>((context, _) =>
	{
		CustomerContextSeed.SeedCustomerAsync(context, Log.Logger).Wait();
	}).Run();

}
catch (Exception ex)
{
	Log.Fatal(ex, "Unhandled exception");
}
finally
{
	Log.Information("Shut down Customer API complete");
	Log.CloseAndFlush();
}


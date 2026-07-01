using Contracts.Common.Interfaces;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Common.Interfaces;
using Ordering.Infrastructure.Persistence;
using Ordering.Infrastructure.Repositories;
using Ordering.Infrastructure.Services;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("DefaultConnectionString")
			?? throw new InvalidOperationException("Connection string 'DefaultConnectionString' is missing.");

		services.AddDbContext<OrderContext>(options =>
			options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("Ordering.Infrastructure")));

		services.AddScoped(typeof(IRepositoryBaseAsync<,,>), typeof(RepositoryBaseAsync<,,>));
		services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
		services.AddScoped<IOrderRepository, OrderRepository>();
		services.AddScoped<ISmtpEmailService, SmtpEmailService>();

		return services;
	}
}

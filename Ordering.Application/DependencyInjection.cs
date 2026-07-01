using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Mappings;

namespace Ordering.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		var assembly = Assembly.GetExecutingAssembly();

		services.AddAutoMapper(cfg => cfg.AddProfile(new OrderingMappingProfile()));
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
		services.AddValidatorsFromAssembly(assembly);

		return services;
	}
}

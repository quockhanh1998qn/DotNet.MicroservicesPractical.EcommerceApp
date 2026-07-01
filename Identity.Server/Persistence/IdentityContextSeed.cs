using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Identity.Server.Configuration;
using Identity.Server.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Server.Persistence;

public static class IdentityContextSeed
{
	public const string AdminRole = "Admin";
	public const string CustomerRole = "Customer";

	public static async Task SeedAsync(IServiceProvider services, ILogger logger)
	{
		await services.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
		var configContext = services.GetRequiredService<ConfigurationDbContext>();
		await configContext.Database.MigrateAsync();
		var identityContext = services.GetRequiredService<IdentityContext>();
		await identityContext.Database.MigrateAsync();

		await SeedIdentityServerConfigurationAsync(configContext);
		await SeedUsersAndRolesAsync(services);
		await SeedPermissionsAsync(identityContext);

		logger.LogInformation("Identity Server seeding completed.");
	}

	private static async Task SeedIdentityServerConfigurationAsync(ConfigurationDbContext context)
	{
		if (!context.Clients.Any())
		{
			foreach (var client in Config.Clients)
			{
				context.Clients.Add(client.ToEntity());
			}
		}

		if (!context.IdentityResources.Any())
		{
			foreach (var resource in Config.IdentityResources)
			{
				context.IdentityResources.Add(resource.ToEntity());
			}
		}

		if (!context.ApiResources.Any())
		{
			foreach (var resource in Config.ApiResources)
			{
				context.ApiResources.Add(resource.ToEntity());
			}
		}

		if (!context.ApiScopes.Any())
		{
			foreach (var scope in Config.ApiScopes)
			{
				context.ApiScopes.Add(scope.ToEntity());
			}
		}

		await context.SaveChangesAsync();
	}

	private static async Task SeedUsersAndRolesAsync(IServiceProvider services)
	{
		var roleManager = services.GetRequiredService<RoleManager<Role>>();
		var userManager = services.GetRequiredService<UserManager<User>>();

		foreach (var roleName in new[] { AdminRole, CustomerRole })
		{
			if (!await roleManager.RoleExistsAsync(roleName))
			{
				await roleManager.CreateAsync(new Role
				{
					Name = roleName,
					NormalizedName = roleName.ToUpperInvariant(),
					Description = $"{roleName} role",
				});
			}
		}

		const string adminEmail = "admin@tedu.local";
		if (await userManager.FindByEmailAsync(adminEmail) is null)
		{
			var admin = new User
			{
				UserName = adminEmail,
				Email = adminEmail,
				EmailConfirmed = true,
				FirstName = "System",
				LastName = "Admin",
				IsActive = true,
			};
			var result = await userManager.CreateAsync(admin, "Admin@123!");
			if (result.Succeeded)
			{
				await userManager.AddToRoleAsync(admin, AdminRole);
			}
		}

		const string customerEmail = "customer@tedu.local";
		if (await userManager.FindByEmailAsync(customerEmail) is null)
		{
			var customer = new User
			{
				UserName = customerEmail,
				Email = customerEmail,
				EmailConfirmed = true,
				FirstName = "Demo",
				LastName = "Customer",
				IsActive = true,
			};
			var result = await userManager.CreateAsync(customer, "Customer@123!");
			if (result.Succeeded)
			{
				await userManager.AddToRoleAsync(customer, CustomerRole);
			}
		}
	}

	private static async Task SeedPermissionsAsync(IdentityContext context)
	{
		var adminRoleId = await context.Roles
			.Where(r => r.Name == AdminRole)
			.Select(r => (long?)r.Id)
			.FirstOrDefaultAsync();
		var customerRoleId = await context.Roles
			.Where(r => r.Name == CustomerRole)
			.Select(r => (long?)r.Id)
			.FirstOrDefaultAsync();

		if (adminRoleId is null || customerRoleId is null)
		{
			return;
		}

		var seed = new List<Permission>
		{
			new() { RoleId = adminRoleId.Value, Function = "product",   Command = "Read"  },
			new() { RoleId = adminRoleId.Value, Function = "product",   Command = "Write" },
			new() { RoleId = adminRoleId.Value, Function = "customer",  Command = "Read"  },
			new() { RoleId = adminRoleId.Value, Function = "customer",  Command = "Write" },
			new() { RoleId = adminRoleId.Value, Function = "inventory", Command = "Read"  },
			new() { RoleId = adminRoleId.Value, Function = "inventory", Command = "Write" },
			new() { RoleId = adminRoleId.Value, Function = "ordering",  Command = "Read"  },
			new() { RoleId = adminRoleId.Value, Function = "hangfire",  Command = "Read"  },

			new() { RoleId = customerRoleId.Value, Function = "product",  Command = "Read"  },
			new() { RoleId = customerRoleId.Value, Function = "basket",   Command = "Read"  },
			new() { RoleId = customerRoleId.Value, Function = "basket",   Command = "Write" },
			new() { RoleId = customerRoleId.Value, Function = "ordering", Command = "Read"  },
		};

		foreach (var permission in seed)
		{
			var exists = await context.Permissions.AnyAsync(p =>
				p.RoleId == permission.RoleId &&
				p.Function == permission.Function &&
				p.Command == permission.Command);

			if (!exists)
			{
				context.Permissions.Add(permission);
			}
		}

		await context.SaveChangesAsync();
	}
}

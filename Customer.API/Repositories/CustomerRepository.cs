using Contracts.Common.Interfaces;
using Customer.API.Entities;
using Customer.API.Persistence;
using Customer.API.Repositories.Interfaces;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Customer.API.Repositories;

public class CustomerRepository : RepositoryBaseAsync<CustomerEntity, long, CustomerContext>, ICustomerRepository
{
	public CustomerRepository(CustomerContext dbContext, IUnitOfWork<CustomerContext> unitOfWork)
		: base(dbContext, unitOfWork)
	{
	}

	public async Task<IEnumerable<CustomerEntity>> GetCustomers() => await FindAll().ToListAsync();

	public Task<CustomerEntity?> GetCustomer(long id) => GetByIdAsync(id);

	public Task<CustomerEntity?> GetCustomerByEmail(string email) =>
		FindByCondition(x => x.Email.Equals(email)).SingleOrDefaultAsync();

	public Task CreateCustomer(CustomerEntity customer) => CreateAsync(customer);

	public Task UpdateCustomer(CustomerEntity customer) => UpdateAsync(customer);

	public async Task DeleteCustomer(long id)
	{
		var customer = await GetCustomer(id);
		if (customer != null)
		{
			await DeleteAsync(customer);
		}
	}
}

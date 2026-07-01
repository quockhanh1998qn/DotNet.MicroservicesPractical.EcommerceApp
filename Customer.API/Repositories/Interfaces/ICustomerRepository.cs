using Contracts.Common.Interfaces;
using Customer.API.Entities;
using Customer.API.Persistence;

namespace Customer.API.Repositories.Interfaces;

public interface ICustomerRepository : IRepositoryBaseAsync<CustomerEntity, long, CustomerContext>
{
	Task<IEnumerable<CustomerEntity>> GetCustomers();

	Task<CustomerEntity?> GetCustomer(long id);

	Task<CustomerEntity?> GetCustomerByEmail(string email);

	Task CreateCustomer(CustomerEntity customer);

	Task UpdateCustomer(CustomerEntity customer);

	Task DeleteCustomer(long id);
}

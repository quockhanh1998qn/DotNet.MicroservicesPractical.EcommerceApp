using Contracts.Common.Interfaces;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Product.API.Entities;
using Product.API.Persistence;
using Product.API.Repositories.Interfaces;

namespace Product.API.Repositories;

public class ProductRepository : RepositoryBaseAsync<CatalogProduct, long, ProductContext>, IProductRepository
{
	public ProductRepository(ProductContext dbContext, IUnitOfWork<ProductContext> unitOfWork) : base(dbContext, unitOfWork)
	{

	}

	public async Task<IEnumerable<CatalogProduct>> GetProducts() => await FindAll().ToListAsync();

	public async Task<(IReadOnlyList<CatalogProduct> Items, long TotalItems)> GetProductsPagedAsync(int pageNumber, int pageSize, string? search)
	{
		if (pageNumber < 1) pageNumber = 1;
		if (pageSize < 1) pageSize = 10;

		var query = FindAll();
		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			query = query.Where(p => EF.Functions.Like(p.Name, $"%{term}%") || EF.Functions.Like(p.No, $"%{term}%"));
		}

		var total = await query.LongCountAsync();
		var items = await query
			.OrderBy(p => p.Id)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		return (items, total);
	}

	public Task<CatalogProduct?> GetProduct(long id) => GetByIdAsync(id);

	public Task<CatalogProduct?> GetProductByNo(string productNo) =>
		FindByCondition(x => x.No.Equals(productNo)).SingleOrDefaultAsync();

	public Task CreateProduct(CatalogProduct product) => CreateAsync(product);

	public Task UpdateProduct(CatalogProduct product) => UpdateAsync(product);

	public async Task DeleteProduct(long id)
	{
		var product = await GetProduct(id);
		if (product != null)
		{
			await DeleteAsync(product);
		}
	}
}

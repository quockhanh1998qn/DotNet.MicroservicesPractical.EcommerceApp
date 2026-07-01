using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Product.API;
using Product.API.Controllers;
using Product.API.Entities;
using Product.API.Repositories.Interfaces;
using Shared.DTOs.Product;
using System.Linq.Expressions;

namespace Product.API.UnitTests;

public class ProductsControllerTests
{
	private static readonly IMapper Mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

	[Fact]
	public async Task CreateProduct_ReturnsCreatedResult_WithProductDto()
	{
		var repository = new InMemoryProductRepository();
		var controller = new ProductsController(repository, Mapper);

		var request = new CreateProductDto
		{
			No = "PRD-UT-001",
			Name = "Unit Test Product",
			Summary = "summary",
			Description = "description",
			Price = 10
		};

		var result = await controller.CreateProduct(request);

		var createdAt = Assert.IsType<CreatedAtRouteResult>(result.Result);
		var dto = Assert.IsType<ProductDto>(createdAt.Value);
		Assert.Equal("PRD-UT-001", dto.No);
		Assert.Equal(1, repository.SaveChangesCallCount);
	}

	[Fact]
	public async Task UpdateProduct_WhenEntityMissing_ReturnsNotFound()
	{
		var repository = new InMemoryProductRepository();
		var controller = new ProductsController(repository, Mapper);

		var request = new UpdateProductDto
		{
			No = "PRD-UT-404",
			Name = "Missing",
			Summary = "missing",
			Description = "missing",
			Price = 20
		};

		var result = await controller.UpdateProduct(999, request);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task DeleteProduct_WhenEntityMissing_ReturnsNotFound()
	{
		var repository = new InMemoryProductRepository();
		var controller = new ProductsController(repository, Mapper);

		var result = await controller.DeleteProduct(123);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task DeleteProduct_WhenEntityExists_ReturnsNoContent_AndPersistsDelete()
	{
		var repository = new InMemoryProductRepository();
		await repository.CreateProduct(new CatalogProduct
		{
			Id = 5,
			No = "PRD-DEL-1",
			Name = "Delete Me",
			Summary = "summary",
			Description = "description",
			Price = 100
		});

		var controller = new ProductsController(repository, Mapper);

		var result = await controller.DeleteProduct(5);

		Assert.IsType<NoContentResult>(result);
		Assert.Equal(1, repository.SaveChangesCallCount);
		Assert.Null(await repository.GetProduct(5));
	}

	[Fact]
	public void MappingProfile_MapsCreateProductDto_ToCatalogProduct()
	{
		var source = new CreateProductDto
		{
			No = "MAP-1",
			Name = "Mapped Name",
			Summary = "Mapped Summary",
			Description = "Mapped Description",
			Price = 88.5m
		};

		var entity = Mapper.Map<CatalogProduct>(source);

		Assert.Equal(source.No, entity.No);
		Assert.Equal(source.Name, entity.Name);
		Assert.Equal(source.Summary, entity.Summary);
		Assert.Equal(source.Description, entity.Description);
		Assert.Equal(source.Price, entity.Price);
	}

	private sealed class InMemoryProductRepository : IProductRepository
	{
		private readonly Dictionary<long, CatalogProduct> _products = new();
		private long _nextId = 1;

		public int SaveChangesCallCount { get; private set; }

		public Task<IEnumerable<CatalogProduct>> GetProducts() => Task.FromResult<IEnumerable<CatalogProduct>>(_products.Values.ToList());

		public Task<CatalogProduct?> GetProduct(long id)
		{
			_products.TryGetValue(id, out var product);
			return Task.FromResult(product);
		}

		public Task<CatalogProduct?> GetProductByNo(string productNo)
		{
			var product = _products.Values.FirstOrDefault(x => x.No.Equals(productNo, StringComparison.Ordinal));
			return Task.FromResult(product);
		}

		public Task CreateProduct(CatalogProduct product)
		{
			if (product.Id == 0)
			{
				product.Id = _nextId++;
			}

			_products[product.Id] = product;
			return Task.CompletedTask;
		}

		public Task UpdateProduct(CatalogProduct product)
		{
			_products[product.Id] = product;
			return Task.CompletedTask;
		}

		public Task DeleteProduct(long id)
		{
			_products.Remove(id);
			return Task.CompletedTask;
		}

		public Task<int> SaveChangesAsync()
		{
			SaveChangesCallCount++;
			return Task.FromResult(1);
		}

		public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotImplementedException();

		public Task EndTransactionAsync() => throw new NotImplementedException();

		public Task RollbackTransactionAsync() => throw new NotImplementedException();

		public IQueryable<CatalogProduct> FindAll(bool trackChanges = false) => _products.Values.AsQueryable();

		public IQueryable<CatalogProduct> FindAll(bool trackChanges = false, params Expression<Func<CatalogProduct, object>>[] includeProperties) => _products.Values.AsQueryable();

		public IQueryable<CatalogProduct> FindByCondition(Expression<Func<CatalogProduct, bool>> expression, bool trackChanges = false)
			=> _products.Values.AsQueryable().Where(expression);

		public IQueryable<CatalogProduct> FindByCondition(Expression<Func<CatalogProduct, bool>> expression, bool trackChanges = false, params Expression<Func<CatalogProduct, object>>[] includeProperties)
			=> _products.Values.AsQueryable().Where(expression);

		public Task<CatalogProduct?> GetByIdAsync(long id) => GetProduct(id);

		public Task<CatalogProduct?> GetByIdAsync(long id, params Expression<Func<CatalogProduct, object>>[] includeProperties) => GetProduct(id);

		public Task<long> CreateAsync(CatalogProduct entity)
		{
			if (entity.Id == 0)
			{
				entity.Id = _nextId++;
			}

			_products[entity.Id] = entity;
			return Task.FromResult(entity.Id);
		}

		public Task<IList<long>> CreateListAsync(IEnumerable<CatalogProduct> entities)
		{
			var ids = new List<long>();
			foreach (var entity in entities)
			{
				if (entity.Id == 0)
				{
					entity.Id = _nextId++;
				}

				_products[entity.Id] = entity;
				ids.Add(entity.Id);
			}

			return Task.FromResult<IList<long>>(ids);
		}

		public Task UpdateAsync(CatalogProduct entity)
		{
			_products[entity.Id] = entity;
			return Task.CompletedTask;
		}

		public Task UpdateListAsync(IEnumerable<CatalogProduct> entities)
		{
			foreach (var entity in entities)
			{
				_products[entity.Id] = entity;
			}

			return Task.CompletedTask;
		}

		public Task DeleteAsync(CatalogProduct entity)
		{
			_products.Remove(entity.Id);
			return Task.CompletedTask;
		}

		public Task DeleteListAsync(IEnumerable<CatalogProduct> entities)
		{
			foreach (var entity in entities)
			{
				_products.Remove(entity.Id);
			}

			return Task.CompletedTask;
		}
	}
}

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Product.API.Entities;
using Product.API.Repositories.Interfaces;
using Shared.DTOs.Product;

namespace Product.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProductsController : ControllerBase
	{
		private readonly IProductRepository _productRepository;
		private readonly IMapper _mapper;

		public ProductsController(IProductRepository productRepository, IMapper mapper)
		{
			_productRepository = productRepository;
			_mapper = mapper;
		}

		[HttpGet]
		public async Task<IActionResult> GetProducts([FromQuery] int? pageNumber, [FromQuery] int? pageSize, [FromQuery] string? search)
		{
			// Back-compat: when no paging args supplied, return the flat list.
			if (pageNumber is null && pageSize is null && string.IsNullOrEmpty(search))
			{
				var all = await _productRepository.GetProducts();
				return Ok(_mapper.Map<IEnumerable<ProductDto>>(all));
			}

			var pn = pageNumber.GetValueOrDefault(1);
			var ps = pageSize.GetValueOrDefault(10);
			var (items, total) = await _productRepository.GetProductsPagedAsync(pn, ps, search);
			var data = _mapper.Map<IEnumerable<ProductDto>>(items);
			var totalPages = ps <= 0 ? 0 : (int)Math.Ceiling(total / (double)ps);
			return Ok(new
			{
				pageNumber = pn,
				pageSize = ps,
				totalRecords = total,
				totalPages,
				data,
			});
		}

		[HttpGet("{id:long}", Name = nameof(GetProductById))]
		[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ProductDto>> GetProductById([FromRoute] long id)
		{
			var product = await _productRepository.GetProduct(id);
			if (product == null)
				return NotFound();
			return Ok(_mapper.Map<ProductDto>(product));
		}

		[HttpGet("no/{productNo}")]
		[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ProductDto>> GetProductByNo([FromRoute] string productNo)
		{
			var product = await _productRepository.GetProductByNo(productNo);
			if (product == null)
				return NotFound();
			return Ok(_mapper.Map<ProductDto>(product));
		}

		[HttpPost]
		[ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
		{
			if (!ModelState.IsValid)
			{
				return ValidationProblem(ModelState);
			}

			var product = _mapper.Map<CatalogProduct>(createProductDto);
			await _productRepository.CreateProduct(product);
			await _productRepository.SaveChangesAsync();

			var result = _mapper.Map<ProductDto>(product);
			return CreatedAtRoute(nameof(GetProductById), new { id = product.Id }, result);
		}

		[HttpPut("{id:long}")]
		[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ProductDto>> UpdateProduct([FromRoute] long id, [FromBody] UpdateProductDto updateProductDto)
		{
			if (!ModelState.IsValid)
			{
				return ValidationProblem(ModelState);
			}

			var existing = await _productRepository.GetProduct(id);
			if (existing == null)
				return NotFound();

			_mapper.Map(updateProductDto, existing);
			await _productRepository.UpdateProduct(existing);
			await _productRepository.SaveChangesAsync();

			return Ok(_mapper.Map<ProductDto>(existing));
		}

		[HttpDelete("{id:long}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> DeleteProduct([FromRoute] long id)
		{
			var product = await _productRepository.GetProduct(id);
			if (product == null)
				return NotFound();
			await _productRepository.DeleteProduct(id);
			await _productRepository.SaveChangesAsync();
			return NoContent();
		}
	}
}

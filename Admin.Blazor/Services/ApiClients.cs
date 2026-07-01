using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Admin.Blazor.Services;

public class AdminProduct
{
	public long Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string No { get; set; } = string.Empty;
	public string? Summary { get; set; }
	public string? Description { get; set; }
	public decimal Price { get; set; }
}

public class AdminPagedResult<T>
{
	[JsonPropertyName("data")] public List<T> Data { get; set; } = new();
	[JsonPropertyName("pageNumber")] public int PageNumber { get; set; }
	[JsonPropertyName("pageSize")] public int PageSize { get; set; }
	[JsonPropertyName("totalRecords")] public long TotalRecords { get; set; }
	[JsonPropertyName("totalPages")] public int TotalPages { get; set; }
}

public record AdminCustomer(long Id, string FirstName, string LastName, string Email);
public record AdminInventory(string Id, string ItemNo, int Quantity);
public record AdminOrder(long Id, string UserName, string FirstName, string LastName, string EmailAddress, decimal TotalPrice);

public class ProductApiClient
{
	private const string Base = "Products";
	private readonly HttpClient _http;
	public ProductApiClient(HttpClient http) => _http = http;

	public Task<AdminPagedResult<AdminProduct>?> ListAsync(int pageNumber = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
	{
		var qs = $"{Base}?pageNumber={pageNumber}&pageSize={pageSize}" + (string.IsNullOrWhiteSpace(search) ? string.Empty : $"&search={Uri.EscapeDataString(search)}");
		return _http.GetFromJsonAsync<AdminPagedResult<AdminProduct>>(qs, ct);
	}

	public Task<AdminProduct?> GetAsync(long id, CancellationToken ct = default) => _http.GetFromJsonAsync<AdminProduct>($"{Base}/{id}", ct);
	public Task<HttpResponseMessage> CreateAsync(AdminProduct product, CancellationToken ct = default) => _http.PostAsJsonAsync(Base, product, ct);
	public Task<HttpResponseMessage> UpdateAsync(AdminProduct product, CancellationToken ct = default) => _http.PutAsJsonAsync($"{Base}/{product.Id}", product, ct);
	public Task<HttpResponseMessage> DeleteAsync(long id, CancellationToken ct = default) => _http.DeleteAsync($"{Base}/{id}");
}

public class CustomerApiClient
{
	private const string Base = "Customers";
	private readonly HttpClient _http;
	public CustomerApiClient(HttpClient http) => _http = http;
	public Task<IReadOnlyList<AdminCustomer>?> ListAsync(CancellationToken ct = default) => _http.GetFromJsonAsync<IReadOnlyList<AdminCustomer>>(Base, ct);
	public Task<AdminCustomer?> GetByUserNameAsync(string userName, CancellationToken ct = default) => _http.GetFromJsonAsync<AdminCustomer>($"{Base}/username/{Uri.EscapeDataString(userName)}", ct);
}

public class OrderApiClient
{
	private const string Base = "Orders";
	private readonly HttpClient _http;
	public OrderApiClient(HttpClient http) => _http = http;
	public Task<IReadOnlyList<AdminOrder>?> ListByUserAsync(string userName, CancellationToken ct = default) => _http.GetFromJsonAsync<IReadOnlyList<AdminOrder>>($"{Base}/{Uri.EscapeDataString(userName)}", ct);
}

public class InventoryApiClient
{
	private const string Base = "Inventories";
	private readonly HttpClient _http;
	public InventoryApiClient(HttpClient http) => _http = http;
	public Task<IReadOnlyList<AdminInventory>?> ListByItemAsync(string itemNo, CancellationToken ct = default) => _http.GetFromJsonAsync<IReadOnlyList<AdminInventory>>($"{Base}/{Uri.EscapeDataString(itemNo)}", ct);
}

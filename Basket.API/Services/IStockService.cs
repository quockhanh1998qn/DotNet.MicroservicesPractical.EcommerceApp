namespace Basket.API.Services;

public interface IStockService
{
	Task<int> GetStockAsync(string itemNo, CancellationToken cancellationToken = default);
}

using Inventory.Grpc.Protos;

namespace Basket.API.Services;

public class StockService : IStockService
{
	private readonly StockProtoService.StockProtoServiceClient _client;
	private readonly ILogger<StockService> _logger;

	public StockService(StockProtoService.StockProtoServiceClient client, ILogger<StockService> logger)
	{
		_client = client;
		_logger = logger;
	}

	public async Task<int> GetStockAsync(string itemNo, CancellationToken cancellationToken = default)
	{
		try
		{
			var response = await _client.GetStockAsync(new GetStockRequest { ItemNo = itemNo }, cancellationToken: cancellationToken);
			return response.Quantity;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "gRPC GetStock failed for {ItemNo}; defaulting to 0", itemNo);
			return 0;
		}
	}
}

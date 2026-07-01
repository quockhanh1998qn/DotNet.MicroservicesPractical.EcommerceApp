using Grpc.Core;
using Inventory.Grpc.Protos;

namespace Inventory.API.Services;

public class StockProtoServiceImpl : StockProtoService.StockProtoServiceBase
{
	private readonly IInventoryService _inventoryService;
	private readonly ILogger<StockProtoServiceImpl> _logger;

	public StockProtoServiceImpl(IInventoryService inventoryService, ILogger<StockProtoServiceImpl> logger)
	{
		_inventoryService = inventoryService;
		_logger = logger;
	}

	public override async Task<StockModel> GetStock(GetStockRequest request, ServerCallContext context)
	{
		if (string.IsNullOrWhiteSpace(request.ItemNo))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "item_no is required."));
		}

		var quantity = await _inventoryService.GetStockAsync(request.ItemNo, context.CancellationToken);
		_logger.LogInformation("gRPC GetStock {ItemNo} => {Quantity}", request.ItemNo, quantity);

		return new StockModel
		{
			ItemNo = request.ItemNo,
			Quantity = quantity
		};
	}
}

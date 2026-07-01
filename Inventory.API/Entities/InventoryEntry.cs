using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Inventory.API.Entities;

public class InventoryEntry
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

	[BsonElement("itemNo")]
	public string ItemNo { get; set; } = string.Empty;

	[BsonElement("quantity")]
	public int Quantity { get; set; }

	[BsonElement("documentNo")]
	public string DocumentNo { get; set; } = string.Empty;

	[BsonElement("documentType")]
	public string DocumentType { get; set; } = string.Empty;

	[BsonElement("externalDocumentNo")]
	public string ExternalDocumentNo { get; set; } = string.Empty;

	[BsonElement("createdDate")]
	public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
}

public static class InventoryDocumentTypes
{
	public const string Purchase = "Purchase";
	public const string Sales = "Sales";
}

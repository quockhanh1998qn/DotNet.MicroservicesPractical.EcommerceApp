namespace Shared.Common;

public class PagedList<T>
{
	public IReadOnlyList<T> Items { get; }
	public int PageNumber { get; }
	public int PageSize { get; }
	public long TotalItems { get; }
	public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
	public bool HasPrevious => PageNumber > 1;
	public bool HasNext => PageNumber < TotalPages;

	public PagedList(IReadOnlyList<T> items, long totalItems, int pageNumber, int pageSize)
	{
		Items = items;
		TotalItems = totalItems;
		PageNumber = pageNumber < 1 ? 1 : pageNumber;
		PageSize = pageSize < 1 ? 10 : pageSize;
	}
}

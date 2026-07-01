export interface Product {
  id: number;
  no: string;
  name: string;
  summary?: string;
  description?: string;
  price: number;
}

export interface PagedResult<T> {
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  data: T[];
}

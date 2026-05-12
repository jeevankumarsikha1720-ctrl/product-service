// Mirrors the C# ProductDto in src/ProductService.Application/Products/Dtos.
// stockQuantity is now sourced from InventoryItem.Available (live), not the
// legacy Product.StockQuantity column. onHand/reserved expose the split for
// admin views that want to see what's physically present vs held in carts.
export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  stockQuantity: number;   // = Available (OnHand - Reserved)
  onHand: number;
  reserved: number;
  isLowStock: boolean;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface CreateProductInput {
  name: string;
  description: string;
  price: number;
  currency: string;
  stockQuantity: number;
}

export interface UpdateProductInput {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  stockQuantity: number;
}

export interface ApiError {
  status: number;
  title: string;
  errors?: Array<{ propertyName: string; errorMessage: string }>;
  traceId?: string;
}

// Mirrors InventoryItemDto on the backend.
export interface InventoryItem {
  id: string;
  productId: string;
  onHand: number;
  reserved: number;
  available: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

import type {
  ApiError,
  CreateProductInput,
  InventoryItem,
  PagedResult,
  Product,
  UpdateProductInput,
} from "./types";

// API base URL. Reads from VITE_API_BASE_URL at build time so we can ship
// different builds for dev vs. production without code changes. Falls back
// to the deployed Azure App Service when no env var is set.
const BASE =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  "https://productservice-api-jk2026.azurewebsites.net/api";

async function request<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
    ...init,
  });

  if (!res.ok) {
    let body: ApiError | undefined;
    try {
      body = (await res.json()) as ApiError;
    } catch {
      // Non-JSON error body; fall through.
    }
    throw Object.assign(new Error(body?.title ?? res.statusText), {
      status: res.status,
      apiError: body,
    });
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export const productsApi = {
  list: (params: { page?: number; pageSize?: number; search?: string } = {}) => {
    const qs = new URLSearchParams();
    if (params.page) qs.set("page", String(params.page));
    if (params.pageSize) qs.set("pageSize", String(params.pageSize));
    if (params.search) qs.set("search", params.search);
    const q = qs.toString();
    return request<PagedResult<Product>>(`/products${q ? `?${q}` : ""}`);
  },

  get: (id: string) => request<Product>(`/products/${id}`),

  create: (input: CreateProductInput) =>
    request<Product>("/products", {
      method: "POST",
      body: JSON.stringify(input),
    }),

  update: (input: UpdateProductInput) =>
    request<Product>(`/products/${input.id}`, {
      method: "PUT",
      body: JSON.stringify(input),
    }),

  delete: (id: string) =>
    request<void>(`/products/${id}`, { method: "DELETE" }),
};

// ───────── Inventory ─────────

export const inventoryApi = {
  /** Current OnHand/Reserved/Available for a product. */
  getByProduct: (productId: string) =>
    request<InventoryItem>(`/inventory/products/${productId}`),

  /** Hold N units against an active cart. Throws if Available < quantity. */
  reserve: (productId: string, quantity: number, cartId: string, note?: string) =>
    request<InventoryItem>(`/inventory/products/${productId}/reserve`, {
      method: "POST",
      body: JSON.stringify({ quantity, referenceId: cartId, note }),
    }),

  /** Release a prior reservation. Used when the user removes/decrements items. */
  release: (productId: string, quantity: number, cartId: string, note?: string) =>
    request<InventoryItem>(`/inventory/products/${productId}/release`, {
      method: "POST",
      body: JSON.stringify({ quantity, referenceId: cartId, note }),
    }),

  /**
   * Commit a reservation: Reserved → Sold. Pass an idempotencyKey to make
   * the call safe to retry — server replays the cached response for 24 hours.
   */
  commit: (
    productId: string,
    quantity: number,
    orderId: string,
    idempotencyKey: string,
    note?: string,
  ) =>
    request<InventoryItem>(`/inventory/products/${productId}/commit`, {
      method: "POST",
      headers: { "Idempotency-Key": idempotencyKey },
      body: JSON.stringify({ quantity, orderId, note }),
    }),
};

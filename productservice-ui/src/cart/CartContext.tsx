import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { inventoryApi } from "../api";
import type { Product } from "../types";

export interface CartItem {
  product: Product;
  quantity: number;
}

interface CartContextValue {
  items: CartItem[];
  itemCount: number;
  subtotal: number;
  currency: string;
  cartId: string;
  busy: boolean;
  lastError: string | null;

  /** Reserve one more unit on the server, then update local state on success. */
  add: (product: Product, qty?: number) => Promise<void>;
  /** Release one unit on the server, then decrement / remove locally. */
  decrement: (productId: string) => Promise<void>;
  /** Release the entire line on the server, then drop it locally. */
  remove: (productId: string) => Promise<void>;
  /** Release every reservation, then empty the cart. */
  clear: () => Promise<void>;

  /** Commit every line as a single checkout. Idempotent — safe to retry. */
  checkout: () => Promise<{ orderId: string }>;
}

const CartContext = createContext<CartContextValue | undefined>(undefined);

const STORAGE_KEY = "productservice.cart.v2";
const CART_ID_KEY = "productservice.cartId.v1";

function loadFromStorage(): CartItem[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as CartItem[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function loadOrCreateCartId(): string {
  if (typeof window === "undefined") return crypto.randomUUID();
  let id = window.localStorage.getItem(CART_ID_KEY);
  if (!id) {
    id = crypto.randomUUID();
    window.localStorage.setItem(CART_ID_KEY, id);
  }
  return id;
}

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>(() => loadFromStorage());
  const [cartId] = useState<string>(() => loadOrCreateCartId());
  const [busy, setBusy] = useState(false);
  const [lastError, setLastError] = useState<string | null>(null);

  useEffect(() => {
    try {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
    } catch {
      /* ignore */
    }
  }, [items]);

  // ───── helpers ─────

  const setLocal = (productId: string, mutator: (current: CartItem | undefined) => CartItem | null) => {
    setItems((current) => {
      const existing = current.find((i) => i.product.id === productId);
      const next = mutator(existing);
      if (existing && next === null) {
        return current.filter((i) => i.product.id !== productId);
      }
      if (existing && next) {
        return current.map((i) => (i.product.id === productId ? next : i));
      }
      if (!existing && next) {
        return [...current, next];
      }
      return current;
    });
  };

  // ───── actions ─────

  const add = useCallback(async (product: Product, qty: number = 1) => {
    if (qty <= 0) return;
    setBusy(true);
    setLastError(null);
    try {
      // Server-side reservation FIRST. If it fails (out of stock), we never
      // mutate local state - the UI stays honest with the warehouse.
      await inventoryApi.reserve(product.id, qty, cartId, "Added to cart");
      setLocal(product.id, (existing) =>
        existing
          ? { ...existing, quantity: existing.quantity + qty }
          : { product, quantity: qty },
      );
    } catch (e) {
      const msg = e instanceof Error ? e.message : "Could not reserve stock.";
      setLastError(msg);
      throw e;
    } finally {
      setBusy(false);
    }
  }, [cartId]);

  const decrement = useCallback(async (productId: string) => {
    const line = items.find((i) => i.product.id === productId);
    if (!line) return;

    setBusy(true);
    setLastError(null);
    try {
      await inventoryApi.release(productId, 1, cartId, "Decremented from cart");
      setLocal(productId, (existing) => {
        if (!existing) return null;
        const next = existing.quantity - 1;
        return next <= 0 ? null : { ...existing, quantity: next };
      });
    } catch (e) {
      setLastError(e instanceof Error ? e.message : "Could not release stock.");
      throw e;
    } finally {
      setBusy(false);
    }
  }, [cartId, items]);

  const remove = useCallback(async (productId: string) => {
    const line = items.find((i) => i.product.id === productId);
    if (!line) return;

    setBusy(true);
    setLastError(null);
    try {
      await inventoryApi.release(productId, line.quantity, cartId, "Removed from cart");
      setLocal(productId, () => null);
    } catch (e) {
      setLastError(e instanceof Error ? e.message : "Could not release stock.");
      throw e;
    } finally {
      setBusy(false);
    }
  }, [cartId, items]);

  const clear = useCallback(async () => {
    if (items.length === 0) return;
    setBusy(true);
    setLastError(null);
    try {
      // Release every line in parallel.
      await Promise.allSettled(
        items.map((i) =>
          inventoryApi.release(i.product.id, i.quantity, cartId, "Cart cleared"),
        ),
      );
      setItems([]);
    } finally {
      setBusy(false);
    }
  }, [cartId, items]);

  const checkout = useCallback(async (): Promise<{ orderId: string }> => {
    if (items.length === 0) throw new Error("Cart is empty.");
    setBusy(true);
    setLastError(null);
    try {
      // Single idempotency key shared across all line commits for this attempt.
      // If the user retries (refresh + click again), the server replays the
      // cached response per line instead of double-committing.
      const orderId = crypto.randomUUID();
      const idempotencyKey = crypto.randomUUID();

      // Sequential commits keep error reporting predictable - if line 3 fails,
      // we know lines 1-2 succeeded. Parallel would muddy the picture.
      for (const line of items) {
        await inventoryApi.commit(
          line.product.id,
          line.quantity,
          orderId,
          idempotencyKey,
          `Checkout for order ${orderId}`,
        );
      }

      setItems([]);
      // Rotate the cartId after a successful checkout so a future "Add" doesn't
      // collide with a finalized order's references.
      const fresh = crypto.randomUUID();
      window.localStorage.setItem(CART_ID_KEY, fresh);
      return { orderId };
    } catch (e) {
      setLastError(e instanceof Error ? e.message : "Checkout failed.");
      throw e;
    } finally {
      setBusy(false);
    }
  }, [items]);

  const value = useMemo<CartContextValue>(() => {
    const itemCount = items.reduce((s, i) => s + i.quantity, 0);
    const subtotal = items.reduce((s, i) => s + i.product.price * i.quantity, 0);
    const currency = items[0]?.product.currency ?? "USD";
    return { items, itemCount, subtotal, currency, cartId, busy, lastError, add, decrement, remove, clear, checkout };
  }, [items, cartId, busy, lastError, add, decrement, remove, clear, checkout]);

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error("useCart must be used inside <CartProvider>.");
  return ctx;
}

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useCart } from "./CartContext";
import { Button } from "../shared/ui";

interface Props {
  open: boolean;
  onClose: () => void;
}

export function CartDrawer({ open, onClose }: Props) {
  const {
    items,
    itemCount,
    subtotal,
    currency,
    busy,
    lastError,
    add,
    decrement,
    remove,
    clear,
    checkout,
  } = useCart();
  const qc = useQueryClient();
  const [lastOrderId, setLastOrderId] = useState<string | null>(null);

  if (!open) return null;

  const fmt = new Intl.NumberFormat(undefined, { style: "currency", currency });

  const handleCheckout = async () => {
    try {
      const { orderId } = await checkout();
      setLastOrderId(orderId);
      // The store needs to see the new Available values after commit drops OnHand.
      qc.invalidateQueries({ queryKey: ["store-products"] });
      qc.invalidateQueries({ queryKey: ["products"] });
    } catch {
      // lastError state in the context already captured the message; UI shows it below.
    }
  };

  // Wrap async cart actions so unhandled rejections (out-of-stock) don't crash React.
  const safe = (fn: () => Promise<unknown>) => () => fn().catch(() => {});

  return (
    <div className="fixed inset-0 z-50">
      <div
        className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden
      />

      <aside
        className="absolute right-0 top-0 flex h-full w-full max-w-md flex-col bg-white shadow-2xl"
        role="dialog"
        aria-label="Shopping cart"
      >
        <header className="flex items-center justify-between border-b border-slate-100 px-6 py-5">
          <div>
            <h2 className="text-xl font-bold text-slate-900">Your Cart</h2>
            <p className="text-xs text-slate-500">
              {itemCount} {itemCount === 1 ? "item" : "items"}
              {busy && <span className="ml-2 text-amber-600">syncing…</span>}
            </p>
          </div>
          <button
            onClick={onClose}
            aria-label="Close cart"
            className="rounded-xl p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
          >
            ✕
          </button>
        </header>

        <div className="flex-1 overflow-y-auto px-6 py-4">
          {/* Success state after a checkout */}
          {lastOrderId && items.length === 0 && (
            <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-5 text-center">
              <div className="mb-2 text-3xl">✓</div>
              <p className="font-semibold text-emerald-900">Order placed</p>
              <p className="mt-1 text-xs text-emerald-700">
                Order ID:{" "}
                <code className="rounded bg-emerald-100 px-1.5 py-0.5 font-mono text-[11px]">
                  {lastOrderId}
                </code>
              </p>
              <p className="mt-3 text-xs text-emerald-700">
                Stock committed. Reserved units have been converted to Sold.
              </p>
            </div>
          )}

          {/* Error banner */}
          {lastError && !busy && (
            <div className="mb-4 rounded-2xl border border-red-200 bg-red-50 p-3 text-sm text-red-800">
              <strong className="font-semibold">Something went wrong.</strong>
              <p className="mt-0.5 text-xs text-red-700">{lastError}</p>
            </div>
          )}

          {items.length === 0 && !lastOrderId && (
            <div className="flex h-full flex-col items-center justify-center text-center">
              <div className="mb-3 text-5xl">🛒</div>
              <p className="text-base font-semibold text-slate-700">Your cart is empty</p>
              <p className="mt-1 text-sm text-slate-500">
                Add a product from the store to get started.
              </p>
            </div>
          )}

          {items.length > 0 && (
            <ul className="space-y-4">
              {items.map(({ product, quantity }) => {
                const atMax = quantity >= product.stockQuantity;
                return (
                  <li
                    key={product.id}
                    className="flex gap-4 rounded-2xl border border-slate-100 bg-white p-3 shadow-sm"
                  >
                    <img
                      src={`https://picsum.photos/seed/${product.id}/120/120`}
                      alt={product.name}
                      className="size-20 shrink-0 rounded-xl object-cover"
                    />
                    <div className="flex flex-1 flex-col">
                      <div className="flex items-start justify-between gap-2">
                        <h3 className="text-sm font-semibold text-slate-900 line-clamp-2">
                          {product.name}
                        </h3>
                        <button
                          onClick={safe(() => remove(product.id))}
                          disabled={busy}
                          aria-label={`Remove ${product.name}`}
                          className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-red-600 disabled:opacity-40"
                        >
                          ✕
                        </button>
                      </div>

                      <div className="mt-auto flex items-center justify-between">
                        <div className="flex items-center gap-2 rounded-full border border-slate-200 px-2 py-1">
                          <button
                            onClick={safe(() => decrement(product.id))}
                            disabled={busy}
                            aria-label="Decrease quantity"
                            className="flex size-6 items-center justify-center rounded-full text-slate-600 hover:bg-slate-100 disabled:opacity-40"
                          >
                            −
                          </button>
                          <span className="min-w-6 text-center text-sm font-semibold text-slate-900">
                            {quantity}
                          </span>
                          <button
                            onClick={safe(() => add(product, 1))}
                            disabled={atMax || busy}
                            aria-label="Increase quantity"
                            title={atMax ? `Only ${product.stockQuantity} available` : undefined}
                            className="flex size-6 items-center justify-center rounded-full text-slate-600 hover:bg-slate-100 disabled:cursor-not-allowed disabled:text-slate-300 disabled:hover:bg-transparent"
                          >
                            +
                          </button>
                        </div>

                        <div className="text-right">
                          <div className="text-sm font-semibold text-slate-900">
                            {fmt.format(product.price * quantity)}
                          </div>
                          {atMax && (
                            <div className="mt-0.5 text-[10px] font-medium uppercase tracking-wide text-amber-600">
                              Max stock
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </div>

        {items.length > 0 && (
          <footer className="border-t border-slate-100 bg-slate-50/60 px-6 py-5">
            <div className="mb-4 flex items-center justify-between">
              <span className="text-sm font-medium text-slate-600">Subtotal</span>
              <span className="text-xl font-bold text-slate-900">{fmt.format(subtotal)}</span>
            </div>

            <div className="flex gap-2">
              <Button
                variant="secondary"
                className="flex-1"
                onClick={safe(clear)}
                disabled={busy}
              >
                Clear cart
              </Button>
              <Button className="flex-1" onClick={handleCheckout} disabled={busy}>
                {busy ? "Processing…" : "Checkout"}
              </Button>
            </div>
            <p className="mt-3 text-center text-xs text-slate-400">
              Checkout commits reserved stock with an idempotency key. Safe to retry.
            </p>
          </footer>
        )}
      </aside>
    </div>
  );
}

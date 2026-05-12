import { useQuery, useQueryClient } from "@tanstack/react-query";
import { productsApi } from "../api";
import { Badge, Button, Spinner } from "../shared/ui";
import { useCart } from "../cart/CartContext";

export function StorePage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({
    queryKey: ["store-products"],
    queryFn: () => productsApi.list({ page: 1, pageSize: 20 }),
  });

  const { add, busy } = useCart();

  // Each Add reserves stock server-side. After success, refresh the catalog
  // so the displayed availability reflects the new Reserved count.
  const handleAdd = async (productId: string) => {
    const product = data?.items.find((p) => p.id === productId);
    if (!product) return;
    try {
      await add(product);
      qc.invalidateQueries({ queryKey: ["store-products"] });
    } catch {
      // CartContext already captured the error message; the cart drawer surfaces it.
    }
  };

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner />
      </div>
    );
  }

  return (
    <div>
      {/* Hero */}
      <section className="border-b border-slate-200 bg-white">
        <div className="mx-auto max-w-7xl px-6 py-20">
          <p className="mb-3 text-sm font-semibold uppercase tracking-widest text-blue-600">
            Modern Commerce
          </p>

          <h1 className="max-w-3xl text-5xl font-black tracking-tight text-slate-900">
            Discover premium tech products.
          </h1>

          <p className="mt-6 max-w-2xl text-lg text-slate-600">
            Fast, elegant, and built with modern React + ASP.NET Core architecture.
          </p>
        </div>
      </section>

      {/* Products */}
      <section className="mx-auto max-w-7xl px-6 py-12">
        <div className="mb-8">
          <h2 className="text-2xl font-bold text-slate-900">Featured Products</h2>
          <p className="mt-1 text-sm text-slate-500">
            {data?.totalCount ?? 0} products available
          </p>
        </div>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {data?.items.map((product) => {
            const isOutOfStock = product.stockQuantity <= 0;
            const isAvailable = product.isActive && !isOutOfStock;
            const isLowStock = isAvailable && product.stockQuantity <= 5;

            return (
              <div
                key={product.id}
                className={`group overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm transition hover:shadow-xl ${
                  isAvailable ? "hover:-translate-y-1" : "opacity-75"
                }`}
              >
                <div className="relative h-56 overflow-hidden bg-slate-100">
                  <img
                    src={`https://picsum.photos/seed/${product.id}/600/400`}
                    alt={product.name}
                    className={`h-full w-full object-cover transition duration-300 ${
                      isAvailable
                        ? "group-hover:scale-105"
                        : "grayscale"
                    }`}
                  />
                  {isOutOfStock && product.isActive && (
                    <div className="absolute inset-0 flex items-center justify-center bg-slate-900/50">
                      <span className="rounded-full bg-white px-4 py-1.5 text-sm font-bold uppercase tracking-wide text-slate-900">
                        Sold Out
                      </span>
                    </div>
                  )}
                </div>

                <div className="space-y-4 p-5">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h3 className="text-lg font-bold text-slate-900">{product.name}</h3>
                      <p className="mt-1 line-clamp-2 text-sm text-slate-500">
                        {product.description}
                      </p>
                    </div>

                    {!product.isActive ? (
                      <Badge tone="danger">Unavailable</Badge>
                    ) : isOutOfStock ? (
                      <Badge tone="danger">Out of Stock</Badge>
                    ) : isLowStock ? (
                      <Badge tone="default">Only {product.stockQuantity} left</Badge>
                    ) : (
                      <Badge tone="success">In Stock</Badge>
                    )}
                  </div>

                  <div className="flex items-center justify-between">
                    <div className="text-2xl font-black text-slate-900">
                      {new Intl.NumberFormat(undefined, {
                        style: "currency",
                        currency: product.currency,
                      }).format(product.price)}
                    </div>
                    <Button
                      onClick={() => handleAdd(product.id)}
                      disabled={!isAvailable || busy}
                    >
                      {isOutOfStock ? "Sold Out" : busy ? "..." : "Add"}
                    </Button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </section>
    </div>
  );
}
